using System;
using System.IO;
using System.Linq;
using HSAdaptCheck.Core;
int passed=0,failed=0;
void Test(string name,Action action) { try {action();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+e.Message);failed++;} }
void Assert(bool x) {if(!x)throw new Exception("Assertion failed");}
(HSAdaptCheck.Core.Snapshot s, Configuration c) Fixture() {
    var s=new Snapshot {CapturedUtc="2026-01-01T00:00:00Z",Source="synthetic",Reference=new PlanData {PlanUid="ref",CourseId="course",StructureSetUid="rss",ImageUid="ct",FrameUid="frame1",DosePerFractionGy=2,Fractions=30},Adaptive=new PlanData {PlanUid="adp",CourseId="segment",StructureSetUid="ass",ImageUid="cbct",FrameUid="frame2",DosePerFractionGy=2,Fractions=5,HasDose=true,StructuresExtracted=true,BeamsExtracted=true,Algorithm="SYNTHETIC",GridXmm=2,GridYmm=2,GridZmm=2}};
    s.Adaptive.Structures.Add(new StructureData {Id="PTV",IsEmpty=false,VolumeCc=100});s.Adaptive.Beams.Add(new BeamData {Id="B1",Machine="SYNTHETIC",Energy="synthetic",Mu=100,Xmm=1,Ymm=2,Zmm=3});
    var c=new Configuration {Version="synthetic-1",IndependentlyConfirmed=true,ConfirmationSource="synthetic independent fixture",Reference=new ExpectedIdentity {PlanUid="ref",CourseId="course",StructureSetUid="rss",ImageUid="ct",FrameUid="frame1"},Adaptive=new ExpectedIdentity {PlanUid="adp",CourseId="segment",StructureSetUid="ass",ImageUid="cbct",FrameUid="frame2"},OriginalCourse=new Prescription {DosePerFractionGy=2,Fractions=30},AdaptiveSegment=new Prescription {DosePerFractionGy=2,Fractions=5},DoseToleranceGy=0.001,Algorithm="SYNTHETIC",GridXmm=2,GridYmm=2,GridZmm=2,GridToleranceMm=0.01,BeamCount=1,MuTolerance=0.01,PositionToleranceMm=0.01};
    c.RequiredStructures.Add(new StructureRule {Key="target",AllowedIds={"PTV"}});c.Beams.Add(new BeamRule {Id="B1",Machine="SYNTHETIC",Energy="synthetic",Mu=100,Xmm=1,Ymm=2,Zmm=3});return(s,c);
}
Status Get(Report r,string name) => r.Results.Single(x=>x.Check==name).Status;
Test("Configured exact comparisons; currency stays not verified",()=>{var(s,c)=Fixture();var r=Checker.Run(s,c);Assert(!r.Results.Any(x=>x.Status==Status.FAIL));Assert(Get(r,"Adaptive plan UID")==Status.PASS);Assert(Get(r,"Dose currency")==Status.NOT_VERIFIED);});
Test("Wrong CBCT UID fails despite plan name",()=>{var(s,c)=Fixture();s.Adaptive.ImageUid="wrong";s.Adaptive.PlanId="ADP_CBCT_01";Assert(Get(Checker.Run(s,c),"Adaptive image UID")==Status.FAIL);});
Test("Missing target fails",()=>{var(s,c)=Fixture();s.Adaptive.Structures.Clear();Assert(Get(Checker.Run(s,c),"Structure target")==Status.FAIL);});
Test("Empty target fails",()=>{var(s,c)=Fixture();s.Adaptive.Structures[0].IsEmpty=true;Assert(Get(Checker.Run(s,c),"Structure target")==Status.FAIL);});
Test("Ambiguous aliases fail",()=>{var(s,c)=Fixture();s.Adaptive.Structures.Add(new StructureData {Id="ptv",IsEmpty=false});Assert(Get(Checker.Run(s,c),"Structure target")==Status.FAIL);});
Test("Overlapping mappings fail",()=>{var(s,c)=Fixture();c.RequiredStructures.Add(new StructureRule {Key="other",AllowedIds={"PTV"}});Assert(Get(Checker.Run(s,c),"Structure target")==Status.FAIL);});
Test("Prescription mismatch fails",()=>{var(s,c)=Fixture();s.Adaptive.Fractions=30;s.Adaptive.DosePerFractionGy=3;var r=Checker.Run(s,c);Assert(Get(r,"Adaptive segment planned fractions")==Status.FAIL);Assert(Get(r,"Adaptive segment dose per fraction (Gy)")==Status.FAIL);});
Test("No expected values cannot pass",()=>{var(s,c)=Fixture();var r=Checker.Run(s,new Configuration());Assert(!r.Results.Any(x=>x.Status==Status.PASS));});
Test("Missing algorithm not verified",()=>{var(s,c)=Fixture();s.Adaptive.Algorithm=null;Assert(Get(Checker.Run(s,c),"Dose algorithm")==Status.NOT_VERIFIED);});
Test("Wrong algorithm fails",()=>{var(s,c)=Fixture();s.Adaptive.Algorithm="other";Assert(Get(Checker.Run(s,c),"Dose algorithm")==Status.FAIL);});
Test("Unexpected machine, energy, MU and isocentre fail",()=>{var(s,c)=Fixture();var b=s.Adaptive.Beams[0];b.Machine="wrong";b.Energy="wrong";b.Mu=999;b.Xmm=999;var r=Checker.Run(s,c);foreach(var name in new[]{"Beam B1 machine","Beam B1 energy","Beam B1 MU","Beam B1 isocentre X (mm)"})Assert(Get(r,name)==Status.FAIL);});
Test("Unmapped extra beam fails",()=>{var(s,c)=Fixture();s.Adaptive.Beams.Add(new BeamData {Id="extra"});var r=Checker.Run(s,c);Assert(Get(r,"Treatment beam count")==Status.FAIL);Assert(Get(r,"Unmapped adaptive beam extra")==Status.FAIL);});
Test("Incomplete enumeration cannot pass",()=>{var(s,c)=Fixture();s.Adaptive.StructuresExtracted=false;s.Adaptive.BeamsExtracted=false;var r=Checker.Run(s,c);Assert(Get(r,"Structure target")==Status.NOT_VERIFIED);Assert(Get(r,"Treatment beam count")==Status.NOT_VERIFIED);});
Test("Negative tolerance and NaN cannot pass",()=>{var(s,c)=Fixture();c.MuTolerance=-1;s.Adaptive.GridXmm=double.NaN;var r=Checker.Run(s,c);Assert(Get(r,"Beam B1 MU")==Status.NOT_VERIFIED);Assert(Get(r,"Dose grid X (mm)")==Status.NOT_VERIFIED);});
Test("Missing confirmation prevents configured pass",()=>{var(s,c)=Fixture();c.IndependentlyConfirmed=false;Assert(Get(Checker.Run(s,c),"Adaptive plan UID")==Status.NOT_VERIFIED);});
Test("Same plan fails",()=>{var(s,c)=Fixture();s.Adaptive.PlanUid=s.Reference.PlanUid;Assert(Get(Checker.Run(s,c),"Distinct selected plans")==Status.FAIL);});
Test("Target doses never inferred from global prescription",()=>{var(s,c)=Fixture();c.AdaptiveSegment.Targets.Add(new TargetLevel {StructureKey="target",TotalDoseGy=10});Assert(Get(Checker.Run(s,c),"Adaptive segment target level target")==Status.NOT_VERIFIED);});
Test("Exceptions recorded visibly",()=>{var(s,c)=Fixture();s.Adaptive.ExtractionIssues.Add("Synthetic unsupported getter");Assert(Get(Checker.Run(s,c),"Adaptive extraction")==Status.NOT_VERIFIED);});
Test("HTML encodes untrusted dataset text",()=>{var(s,c)=Fixture();s.Adaptive.PlanId="<script>alert(1)</script>";var html=HtmlReport.Render(Checker.Run(s,c));Assert(!html.Contains("<script>"));Assert(html.Contains("&lt;script&gt;"));});
Test("JSON roundtrip and fingerprint reflect edits",()=>{var(s,c)=Fixture();var r=Checker.Run(s,c);var path=Path.GetTempFileName();try{Json.Write(path,r);var loaded=Json.Read<Report>(path);Assert(loaded.Results.Count==r.Results.Count);Assert(loaded.Dataset.Adaptive.PlanUid=="adp");c.Version="new";Assert(Checker.Run(s,c).ConfigurationSha256!=r.ConfigurationSha256);}finally{File.Delete(path);}});
Test("Null inputs remain visible",()=>{Assert(Checker.Run(null,null).Results.Any(x=>x.Status==Status.NOT_VERIFIED));});
Test("UTF8 BOM configuration accepted",()=>{var(s,c)=Fixture();var path=Path.GetTempFileName();try{File.WriteAllText(path,Json.Serialize(c),new System.Text.UTF8Encoding(true));Assert(Json.Read<Configuration>(path).Version==c.Version);}finally{File.Delete(path);}});
Test("Non-positive dose cannot pass",()=>{var(s,c)=Fixture();s.Adaptive.DosePerFractionGy=-2;c.AdaptiveSegment.DosePerFractionGy=-2;Assert(Get(Checker.Run(s,c),"Adaptive segment dose per fraction (Gy)")==Status.NOT_VERIFIED);});
Test("Zero extracted treatment beams fails positive expectation",()=>{var(s,c)=Fixture();s.Adaptive.Beams.Clear();Assert(Get(Checker.Run(s,c),"Treatment beam count")==Status.FAIL);});
Test("Configured tolerance boundary and exceeded tolerance",()=>{var(s,c)=Fixture();c.MuTolerance=0.5;s.Adaptive.Beams[0].Mu=100.5;Assert(Get(Checker.Run(s,c),"Beam B1 MU")==Status.PASS);s.Adaptive.Beams[0].Mu=100.5001;Assert(Get(Checker.Run(s,c),"Beam B1 MU")==Status.FAIL);});
Test("Absent dose fails and inaccessible dose remains not verified",()=>{var(s,c)=Fixture();s.Adaptive.HasDose=false;Assert(Get(Checker.Run(s,c),"Adaptive dose availability")==Status.FAIL);s.Adaptive.HasDose=null;Assert(Get(Checker.Run(s,c),"Adaptive dose availability")==Status.NOT_VERIFIED);});
Test("Missing beam MU cannot pass",()=>{var(s,c)=Fixture();s.Adaptive.Beams[0].Mu=null;Assert(Get(Checker.Run(s,c),"Beam B1 MU")==Status.NOT_VERIFIED);});
Test("Rationale cannot override a machine discrepancy",()=>{var(s,c)=Fixture();c.ExpectedChangeRationale="Intended reoptimisation";s.Adaptive.Beams[0].Machine="wrong";var report=Checker.Run(s,c);Assert(Get(report,"Beam B1 machine")==Status.FAIL);Assert(Get(report,"Reference-to-adaptive parameters")==Status.REVIEW);});
if(args.Length==1) {var(s,c)=Fixture();Directory.CreateDirectory(args[0]);Json.Write(Path.Combine(args[0],"synthetic-snapshot.json"),s);Json.Write(Path.Combine(args[0],"synthetic-config.json"),c);}
Console.WriteLine($"{passed} passed; {failed} failed; {passed+failed} executed");return failed==0?0:1;
