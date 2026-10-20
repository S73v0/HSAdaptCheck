using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using HSAdaptCheck.Core;
namespace HSAdaptCheck.Esapi
{
    // Read-only reflection boundary: unavailable members produce evidence gaps, never guessed values.
    // All calls run synchronously on the Eclipse script thread. No API object leaves this boundary.
    public static class Extractor
    {
        sealed class Reader
        {
            readonly PlanData data;
            public Reader(PlanData d) { data=d; }
            public object Get(object obj,string path)
            {
                try {
                    foreach(var member in path.Split('.')) {
                        if(obj==null) throw new InvalidOperationException();
                        var p=obj.GetType().GetProperty(member,BindingFlags.Public|BindingFlags.Instance);
                        if(p!=null) obj=p.GetValue(obj,null);
                        else { var f=obj.GetType().GetField(member,BindingFlags.Public|BindingFlags.Instance);if(f==null)throw new MissingMemberException();obj=f.GetValue(obj); }
                    }
                    if(obj==null) data.ExtractionIssues.Add(path+": null");
                    return obj;
                } catch(Exception ex) { data.ExtractionIssues.Add(path+": unavailable ("+ex.GetType().Name+")");return null; }
            }
            public string Str(object o,string path) { var v=Get(o,path);return v==null?null:Convert.ToString(v,CultureInfo.InvariantCulture); }
            public double? Num(object o,string path) { try { var v=Get(o,path);return v==null?(double?)null:Convert.ToDouble(v,CultureInfo.InvariantCulture); } catch { data.ExtractionIssues.Add(path+": invalid numeric data");return null; } }
            public bool? Bool(object o,string path) { var v=Get(o,path);if(v is bool)return (bool)v;data.ExtractionIssues.Add(path+": not boolean");return null; }
        }
        public static PlanData Read(object plan)
        {
            var d=new PlanData();var r=new Reader(d);
            d.PlanUid=r.Str(plan,"UID");d.PlanId=r.Str(plan,"Id");d.CourseId=r.Str(plan,"Course.Id");
            d.StructureSetUid=r.Str(plan,"StructureSet.UID");d.ImageUid=r.Str(plan,"StructureSet.Image.UID");d.FrameUid=r.Str(plan,"StructureSet.Image.FOR");
            var dose=r.Get(plan,"Dose");
            // A null Dose is different from a getter failure; query once through a dedicated guarded block.
            try { var p=plan.GetType().GetProperty("Dose");if(p==null)throw new MissingMemberException();d.HasDose=p.GetValue(plan,null)!=null; } catch { d.HasDose=null;d.ExtractionIssues.Add("Dose availability getter failed"); }
            var f=r.Num(plan,"NumberOfFractions");if(f.HasValue&&f.Value>0&&f.Value<=int.MaxValue&&f.Value==Math.Truncate(f.Value))d.Fractions=(int)f.Value;
            var dp=r.Num(plan,"DosePerFraction.Dose");var unit=r.Str(plan,"DosePerFraction.Unit");
            if(unit=="Gy")d.DosePerFractionGy=dp;else if(unit=="cGy")d.DosePerFractionGy=dp/100.0;else d.ExtractionIssues.Add("DosePerFraction: unsupported/missing unit; relative dose not accepted");
            if(dose!=null) { d.GridXmm=r.Num(dose,"XRes");d.GridYmm=r.Num(dose,"YRes");d.GridZmm=r.Num(dose,"ZRes"); }
            try {
                var method=plan.GetType().GetMethod("GetCalculationModel",new[]{typeof(string)});
                // ESAPI uses CalculationType enum on supported versions; select only that documented enum signature.
                if(method==null) foreach(var m in plan.GetType().GetMethods()) if(m.Name=="GetCalculationModel"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType.IsEnum) { method=m;break; }
                if(method==null)throw new MissingMethodException();
                var type=method.GetParameters()[0].ParameterType;
                if(!type.IsEnum)throw new NotSupportedException();
                d.Algorithm=Convert.ToString(method.Invoke(plan,new[]{Enum.Parse(type,"PhotonVolumeDose")}),CultureInfo.InvariantCulture);
            } catch(Exception ex) { d.ExtractionIssues.Add("PhotonVolumeDose calculation model: unavailable ("+ex.GetType().Name+")"); }
            try {
                var items=r.Get(plan,"StructureSet.Structures") as IEnumerable;if(items==null)throw new InvalidOperationException();
                foreach(var item in items)d.Structures.Add(new StructureData { Id=r.Str(item,"Id"),IsEmpty=r.Bool(item,"IsEmpty"),VolumeCc=r.Num(item,"Volume") });
                d.StructuresExtracted=true;
            } catch { d.StructuresExtracted=false;d.ExtractionIssues.Add("Structure enumeration incomplete"); }
            try {
                var items=r.Get(plan,"Beams") as IEnumerable;if(items==null)throw new InvalidOperationException();
                foreach(var item in items) {
                    var setup=r.Bool(item,"IsSetupField");if(!setup.HasValue)throw new InvalidOperationException();if(setup==true)continue;
                    d.Beams.Add(new BeamData {Id=r.Str(item,"Id"),Machine=r.Str(item,"TreatmentUnit.Id"),Energy=r.Str(item,"EnergyModeDisplayName"),Mu=r.Num(item,"Meterset.Value"),Xmm=r.Num(item,"IsocenterPosition.x"),Ymm=r.Num(item,"IsocenterPosition.y"),Zmm=r.Num(item,"IsocenterPosition.z")});
                }
                d.BeamsExtracted=true;
            } catch { d.BeamsExtracted=false;d.ExtractionIssues.Add("Treatment beam enumeration incomplete"); }
            return d;
        }
    }
}
