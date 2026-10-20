using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
namespace HSAdaptCheck.Core
{
    public sealed class PlanData
    {
        public string PlanUid { get; set; }
        public string PlanId { get; set; }
        public string CourseId { get; set; }
        public string StructureSetUid { get; set; }
        public string ImageUid { get; set; }
        public string FrameUid { get; set; }
        public bool? HasDose { get; set; }
        public double? DosePerFractionGy { get; set; }
        public int? Fractions { get; set; }
        public string Algorithm { get; set; }
        // Only populate when the API semantics and units have been commissioned.
        public double? GridXmm { get; set; }
        public double? GridYmm { get; set; }
        public double? GridZmm { get; set; }
        public List<StructureData> Structures { get; set; } = new List<StructureData>();
        public List<BeamData> Beams { get; set; } = new List<BeamData>();
        public bool? StructuresExtracted { get; set; }
        public bool? BeamsExtracted { get; set; }
        public List<string> ExtractionIssues { get; set; } = new List<string>();
    }
    public sealed class StructureData
    {
        public string Id { get; set; }
        public bool? IsEmpty { get; set; }
        public double? VolumeCc { get; set; }
    }
    public sealed class BeamData
    {
        public string Id { get; set; }
        public string Machine { get; set; }
        public string Energy { get; set; }
        public double? Mu { get; set; }
        public double? Xmm { get; set; }
        public double? Ymm { get; set; }
        public double? Zmm { get; set; }
    }
    public sealed class ExpectedIdentity
    {
        public string PlanUid { get; set; }
        public string CourseId { get; set; }
        public string StructureSetUid { get; set; }
        public string ImageUid { get; set; }
        public string FrameUid { get; set; }
    }
    public sealed class Prescription
    {
        public double? DosePerFractionGy { get; set; }
        public int? Fractions { get; set; }
        public List<TargetLevel> Targets { get; set; } = new List<TargetLevel>();
    }
    public sealed class TargetLevel
    {
        public string StructureKey { get; set; }
        public double? TotalDoseGy { get; set; }
    }
    public sealed class StructureRule
    {
        public string Key { get; set; }
        public List<string> AllowedIds { get; set; } = new List<string>();
    }
    public sealed class BeamRule
    {
        public string Id { get; set; }
        public string Machine { get; set; }
        public string Energy { get; set; }
        public double? Mu { get; set; }
        public double? Xmm { get; set; }
        public double? Ymm { get; set; }
        public double? Zmm { get; set; }
    }
    public sealed class Configuration
    {
        public string Version { get; set; }
        public string ConfirmationSource { get; set; }
        public bool IndependentlyConfirmed { get; set; }
        public ExpectedIdentity Reference { get; set; }
        public ExpectedIdentity Adaptive { get; set; }
        public Prescription OriginalCourse { get; set; }
        public Prescription AdaptiveSegment { get; set; }
        public double? DoseToleranceGy { get; set; }
        public List<StructureRule> RequiredStructures { get; set; } = new List<StructureRule>();
        public string Algorithm { get; set; }
        public double? GridXmm { get; set; }
        public double? GridYmm { get; set; }
        public double? GridZmm { get; set; }
        public double? GridToleranceMm { get; set; }
        public int? BeamCount { get; set; }
        public List<BeamRule> Beams { get; set; } = new List<BeamRule>();
        public double? MuTolerance { get; set; }
        public double? PositionToleranceMm { get; set; }
        // Explicit acknowledgement only; changes remain REVIEW, never an implicit PASS.
        public string ExpectedChangeRationale { get; set; }
    }
    public enum Status { PASS, REVIEW, FAIL, NOT_VERIFIED }
    public sealed class CheckResult
    {
        public string Check { get; set; }
        public Status Status { get; set; }
        public string Observed { get; set; }
        public string Expected { get; set; }
        public string Reason { get; set; }
        public string Source { get; set; }
    }
    public sealed class Snapshot
    {
        public PlanData Reference { get; set; }
        public PlanData Adaptive { get; set; }
        public string CapturedUtc { get; set; }
        public string Source { get; set; }
    }
    public sealed class Report
    {
        public string CheckerVersion { get; set; } = "0.1.0-prototype";
        public string ConfigurationVersion { get; set; }
        public string ConfigurationSha256 { get; set; }
        public string SnapshotSha256 { get; set; }
        public string CreatedUtc { get; set; }
        public Snapshot Dataset { get; set; }
        public Configuration Configuration { get; set; }
        public List<CheckResult> Results { get; set; } = new List<CheckResult>();
        public string Notice { get; set; } = "DEVELOPMENT PROTOTYPE — physics review support only. Snapshot of extracted data; rerun after any edit. No treatment authorisation. Dose currency, anatomy, approvals, delivery and complete workflow are not verified.";
    }
    public static class Json
    {
        public static T Read<T>(string path)
        {
            using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(path,Encoding.UTF8)))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        public static string Serialize<T>(T value)
        {
            using(var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        public static void Write<T>(string path,T value) { File.WriteAllText(path,Serialize(value),new UTF8Encoding(false)); }
    }
}
