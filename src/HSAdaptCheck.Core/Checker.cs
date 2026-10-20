using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace HSAdaptCheck.Core
{
    public static class Checker
    {
        static bool Text(string x) { return !string.IsNullOrWhiteSpace(x); }
        static string Show(object x) { return x == null ? "unavailable / unconfigured" : Convert.ToString(x,CultureInfo.InvariantCulture); }
        static bool Finite(double? x) { return x.HasValue && !double.IsNaN(x.Value) && !double.IsInfinity(x.Value); }
        static string Hash(string value) { using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant(); }
        public static Report Run(Snapshot snapshot, Configuration config)
        {
            var c=config ?? new Configuration();
            var s=snapshot ?? new Snapshot();
            var r=new Report { CreatedUtc=DateTime.UtcNow.ToString("o"), ConfigurationVersion=c.Version, Dataset=s, Configuration=c,
                ConfigurationSha256=Hash(Json.Serialize(c)), SnapshotSha256=Hash(Json.Serialize(s)) };
            Action<string,Status,object,object,string,string> add=(key,status,obs,exp,reason,source)=>r.Results.Add(new CheckResult { Check=key,Status=status,Observed=Show(obs),Expected=Show(exp),Reason=reason,Source=source });
            bool confirmed=c.IndependentlyConfirmed && Text(c.ConfirmationSource) && Text(c.Version);
            add("Configuration confirmation",confirmed?Status.PASS:Status.NOT_VERIFIED,c.ConfirmationSource,"versioned, independently confirmed expected inputs",confirmed?"User-recorded confirmation; checker cannot establish independence itself.":"Complete and independently confirm configuration before interpreting comparisons.","Configuration metadata");
            Action<string,string,string,string> eq=(key,obs,exp,source)=>add(key,!confirmed||!Text(obs)||!Text(exp)?Status.NOT_VERIFIED:obs==exp?Status.PASS:Status.FAIL,obs,exp,!confirmed?"Expected inputs not confirmed.":!Text(obs)||!Text(exp)?"Missing expected or extracted value.":obs==exp?"Exact match.":"Mismatch.",source);
            Action<string,double?,double?,double?,string> num=(key,obs,exp,tol,source)=> {
                bool requiresPositive=key.Contains("dose per fraction")||key.StartsWith("Dose grid")||key.EndsWith(" MU");
                bool valid=confirmed && Finite(obs)&&Finite(exp)&&Finite(tol)&&tol>=0&&(!requiresPositive||exp>0);
                add(key,!valid?Status.NOT_VERIFIED:requiresPositive&&obs<=0?Status.FAIL:Math.Abs(obs.Value-exp.Value)<=tol.Value?Status.PASS:Status.FAIL,obs,Show(exp)+" ± "+Show(tol),valid?"Compared using configured absolute tolerance.":"Missing/unconfirmed/non-finite value or invalid tolerance.",source);
            };
            Action<string,int?,int?> integer=(key,obs,exp)=>add(key,!confirmed||!obs.HasValue||!exp.HasValue||exp<=0?Status.NOT_VERIFIED:obs>0&&obs==exp?Status.PASS:Status.FAIL,obs,exp,"Exact positive count required.","Saved plan / independently confirmed configuration");
            Action<string,PlanData,ExpectedIdentity> identity=(label,p,e)=> {
                p=p??new PlanData();e=e??new ExpectedIdentity();
                eq(label+" plan UID",p.PlanUid,e.PlanUid,"PlanSetup.UID / expected identity");
                eq(label+" course",p.CourseId,e.CourseId,"PlanSetup.Course.Id / expected identity");
                eq(label+" structure set UID",p.StructureSetUid,e.StructureSetUid,"StructureSet.UID / expected identity");
                eq(label+" image UID",p.ImageUid,e.ImageUid,"Image.UID / independently selected CBCT identity");
                eq(label+" frame UID",p.FrameUid,e.FrameUid,"Image.FOR / expected identity");
            };
            identity("Reference",s.Reference,c.Reference);identity("Adaptive",s.Adaptive,c.Adaptive);
            var reference=s.Reference??new PlanData();var adaptive=s.Adaptive??new PlanData();
            add("Distinct selected plans",!confirmed||!Text(reference.PlanUid)||!Text(adaptive.PlanUid)?Status.NOT_VERIFIED:reference.PlanUid==adaptive.PlanUid?Status.FAIL:Status.PASS,reference.PlanUid+" / "+adaptive.PlanUid,"different UIDs","Selection sanity check; does not prove correct provenance.","Selected saved plans");
            Action<string,PlanData,Prescription> rx=(label,p,e)=> {
                e=e??new Prescription();
                num(label+" dose per fraction (Gy)",p.DosePerFractionGy,e.DosePerFractionGy,c.DoseToleranceGy,"PlanSetup.DosePerFraction converted to Gy / expected prescription");
                integer(label+" planned fractions",p.Fractions,e.Fractions);
                if(e.Targets==null||e.Targets.Count==0) add(label+" target dose levels",Status.NOT_VERIFIED,null,null,"No independently recorded target levels.","Configuration");
                else foreach(var t in e.Targets) add(label+" target level "+(t==null?"(null)":t.StructureKey),Status.NOT_VERIFIED,"Not extracted",t==null?null:t.TotalDoseGy,"Target-level prescription extraction is not implemented; plan-level dose is insufficient for multi-level prescriptions.","Configuration / API limitation");
            };
            rx("Original course",reference,c.OriginalCourse);rx("Adaptive segment",adaptive,c.AdaptiveSegment);
            var structures=adaptive.Structures??new List<StructureData>();
            var rules=c.RequiredStructures??new List<StructureRule>();
            if(rules.Count==0) add("Required structures",Status.NOT_VERIFIED,null,null,"No required structure mapping configured.","Configuration");
            foreach(var rule in rules) {
                if(rule==null||!Text(rule.Key)||rule.AllowedIds==null||rule.AllowedIds.Count==0||rule.AllowedIds.Any(x=>!Text(x))) { add("Structure mapping",Status.NOT_VERIFIED,null,null,"Incomplete mapping.","Configuration");continue; }
                var matches=structures.Where(x=>x!=null&&rule.AllowedIds.Contains(x.Id,StringComparer.OrdinalIgnoreCase)).ToList();
                bool overlap=rules.Count(x=>x!=null&&x.Key==rule.Key)>1 || rules.Any(x=>x!=null&&!object.ReferenceEquals(x,rule)&&x.AllowedIds!=null&&x.AllowedIds.Any(id=>rule.AllowedIds.Contains(id,StringComparer.OrdinalIgnoreCase))) || rule.AllowedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=rule.AllowedIds.Count;
                var status=!confirmed||adaptive.StructuresExtracted!=true?Status.NOT_VERIFIED:overlap||matches.Count>1?Status.FAIL:matches.Count==0?Status.FAIL:!matches[0].IsEmpty.HasValue?Status.NOT_VERIFIED:matches[0].IsEmpty==true?Status.FAIL:Status.PASS;
                add("Structure "+rule.Key,status,string.Join(", ",matches.Select(x=>x.Id+" empty="+Show(x.IsEmpty)+" volume cc="+Show(x.VolumeCc))),string.Join(" OR ",rule.AllowedIds),overlap?"Overlapping, duplicate or ambiguous mapping.":"Requires exactly one non-empty structure. No anatomical correctness inference.","StructureSet.Structures / configured aliases");
            }
            add("Adaptive dose availability",!confirmed||!adaptive.HasDose.HasValue?Status.NOT_VERIFIED:adaptive.HasDose==true?Status.PASS:Status.FAIL,adaptive.HasDose,true,"Availability only; does not establish dose currency.","PlanSetup.Dose != null");
            add("Saved-state provenance",Status.NOT_VERIFIED,"ESAPI context snapshot","Saved planning data independently established","The adapter cannot establish whether pending unsaved edits exist. Follow local save/reopen procedure before capture.","API / v0.1 limitation");
            add("Dose currency",Status.NOT_VERIFIED,"No sufficient evidence extracted","Dose consistent with latest saved inputs","Availability, plan name and timestamps alone cannot establish currency.","API / v0.1 limitation");
            eq("Dose algorithm",adaptive.Algorithm,c.Algorithm,"PlanSetup.GetCalculationModel(PhotonVolumeDose) / configuration");
            num("Dose grid X (mm)",adaptive.GridXmm,c.GridXmm,c.GridToleranceMm,"Dose.XRes / configured grid");
            num("Dose grid Y (mm)",adaptive.GridYmm,c.GridYmm,c.GridToleranceMm,"Dose.YRes / configured grid");
            num("Dose grid Z (mm)",adaptive.GridZmm,c.GridZmm,c.GridToleranceMm,"Dose.ZRes / configured grid");
            var beams=adaptive.Beams??new List<BeamData>();
            integer("Treatment beam count",adaptive.BeamsExtracted==true?(int?)beams.Count:null,c.BeamCount);
            var expectedBeams=c.Beams??new List<BeamRule>();
            if(expectedBeams.Count==0) add("Treatment beam expectations",Status.NOT_VERIFIED,null,null,"No beam expectations configured.","Configuration");
            foreach(var b in expectedBeams) {
                if(b==null||!Text(b.Id)) { add("Beam mapping",Status.NOT_VERIFIED,null,null,"Missing beam ID.","Configuration");continue; }
                var matches=beams.Where(x=>x!=null&&x.Id==b.Id).ToList();
                if(adaptive.BeamsExtracted!=true||matches.Count!=1||expectedBeams.Count(x=>x!=null&&x.Id==b.Id)!=1) {
                    add("Beam "+b.Id,adaptive.BeamsExtracted==true&&confirmed?Status.FAIL:Status.NOT_VERIFIED,matches.Count,1,"Missing/duplicate beam mapping or incomplete extraction.","Treatment beams / configuration");continue;
                }
                var beam=matches[0];eq("Beam "+b.Id+" machine",beam.Machine,b.Machine,"Beam.TreatmentUnit.Id");eq("Beam "+b.Id+" energy",beam.Energy,b.Energy,"Beam.EnergyModeDisplayName");
                num("Beam "+b.Id+" MU",beam.Mu,b.Mu,c.MuTolerance,"Beam.Meterset.Value");
                num("Beam "+b.Id+" isocentre X (mm)",beam.Xmm,b.Xmm,c.PositionToleranceMm,"Beam.IsocenterPosition.x (DICOM)");
                num("Beam "+b.Id+" isocentre Y (mm)",beam.Ymm,b.Ymm,c.PositionToleranceMm,"Beam.IsocenterPosition.y (DICOM)");
                num("Beam "+b.Id+" isocentre Z (mm)",beam.Zmm,b.Zmm,c.PositionToleranceMm,"Beam.IsocenterPosition.z (DICOM)");
            }
            foreach(var b in beams.Where(x=>x!=null&&!expectedBeams.Any(e=>e!=null&&e.Id==x.Id))) add("Unmapped adaptive beam "+b.Id,confirmed?Status.FAIL:Status.NOT_VERIFIED,b.Id,"Explicit configured beam expectation","Unmapped beam; do not assume expected adaptation change.","Saved treatment beams");
            // Report all changes, including added/removed beams, without assuming that equal beam IDs imply equal purpose.
            add("Reference-to-adaptive parameters",Status.REVIEW,"Reference: "+BeamSummary(reference)+" → Adaptive: "+BeamSummary(adaptive),c.ExpectedChangeRationale,"Observed reference → adaptive comparison. "+(Text(c.ExpectedChangeRationale)?"Recorded expected-change rationale: "+c.ExpectedChangeRationale:"No expected-change rationale recorded.")+" Review beam correspondence, coordinate frames and changes independently; this row cannot pass.","Saved treatment beams; setup fields excluded; coordinates in image DICOM frame");
            add("Beam count change",reference.BeamsExtracted==true&&adaptive.BeamsExtracted==true?Status.REVIEW:Status.NOT_VERIFIED,Show(reference.Beams==null?(int?)null:reference.Beams.Count)+" → "+Show(adaptive.Beams==null?(int?)null:adaptive.Beams.Count),c.BeamCount,"Reference → adaptive. Compare against independently confirmed adaptive expectations above.","Treatment beam enumeration");
            foreach(var b in beams.Where(x=>x!=null)) {
                var old=(reference.Beams??new List<BeamData>()).Where(x=>x!=null&&x.Id==b.Id).ToList();
                if(old.Count!=1) {add("Beam change "+b.Id,Status.REVIEW,"No unique reference ID correspondence",null,"Added or ambiguous correspondence; review independently.","Beam IDs");continue;}
                var prev=old[0];
                add("Beam change "+b.Id,Status.REVIEW,"MU delta="+Delta(prev.Mu,b.Mu)+"; iso delta mm=("+Delta(prev.Xmm,b.Xmm)+","+Delta(prev.Ymm,b.Ymm)+","+Delta(prev.Zmm,b.Zmm)+")",c.ExpectedChangeRationale,"Adaptive minus reference, paired by exact beam ID only. Isocentre deltas across different frames are descriptive, not physical displacement. Rationale does not make a discrepancy pass.","Saved treatment beams");
            }
            foreach(var pair in new[]{new {Name="Reference",Plan=reference},new {Name="Adaptive",Plan=adaptive}}) foreach(var issue in pair.Plan.ExtractionIssues??new List<string>()) add(pair.Name+" extraction",Status.NOT_VERIFIED,issue,"Successful extraction","Extraction failure or unsupported property.","ESAPI adapter");
            if(!Text(s.CapturedUtc)||!Text(s.Source)) add("Snapshot provenance",Status.NOT_VERIFIED,s.CapturedUtc,"Capture time and extraction source","Missing snapshot metadata.","Snapshot metadata");
            return r;
        }
        static string Delta(double? a,double? b) { return Finite(a)&&Finite(b)?Show(b.Value-a.Value):"unavailable"; }
        static string BeamSummary(PlanData p)
        {
            return "frame="+Show(p.FrameUid)+"; extracted="+Show(p.BeamsExtracted)+"; "+string.Join("; ",(p.Beams??new List<BeamData>()).Where(x=>x!=null).Select(b=>b.Id+": "+Show(b.Machine)+" / "+Show(b.Energy)+" MU="+Show(b.Mu)+" iso mm=("+Show(b.Xmm)+","+Show(b.Ymm)+","+Show(b.Zmm)+")"));
        }
    }
}
