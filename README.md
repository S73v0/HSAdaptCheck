# HSAdaptCheck 0.1 — development prototype

Read-only physics review support for saved offline adaptive plans on Halcyon / HyperSight CBCT. This tool does not authorise treatment and does not verify the whole adaptive workflow. **Independent physicist review, local commissioning and clinical approval are required before clinical use.**

## Implementation and validation status

The starting repository was empty. No Eclipse runtime, Varian assemblies or Windows desktop runtime are installed on the development machine. The user supplied a minimal `VMS.TPS.Script.Execute(ScriptContext)` scaffold and a working legacy x64 .NET Framework 4.8 project. Its stale RTM 16.1 reference path was updated to RTM 18.2 following the user’s explicit correction. The ESAPI host adapts that project/solution format and entry point, including `NoInlining` and no write access attribute. Actual installed binary versions have not been inspected.

- `src/HSAdaptCheck.Core`: .NET Standard 2.0 DTOs, independent rules and HTML/JSON reporting. No ESAPI dependency.
- `src/HSAdaptCheck.Cli`: .NET 8 snapshot runner for development and synthetic checks.
- `src/HSAdaptCheck.Esapi`: .NET Framework 4.8 x64 WPF script with plan selectors and a guarded read-only extraction boundary. **Not compiled against Varian assemblies or exercised in Eclipse.**
- `tests/HSAdaptCheck.Tests`: dependency-free executable synthetic test suite. Synthetic numerical values are test data, never clinical defaults.

See [validation record](docs/VALIDATION.md), [API limitations](docs/API-LIMITATIONS.md), [configuration guide](docs/CONFIGURATION.md) and [commissioning checklist](docs/COMMISSIONING.md).

## Build and run the independent workflow

Install .NET SDK 8.0.426 (pinned by `global.json`), or an SDK satisfying its patch roll-forward. From the repository root:

```sh
dotnet restore HSAdaptCheck.sln --locked-mode
dotnet build HSAdaptCheck.sln --no-restore -c Release
dotnet run --project tests/HSAdaptCheck.Tests -c Release --no-build -- local/synthetic
dotnet run --project src/HSAdaptCheck.Cli -c Release --no-build -- local/synthetic/synthetic-snapshot.json local/synthetic/synthetic-config.json local/report-001
```

The solution deliberately includes only the portable components. The test command executes assertions and prints the number passed/failed; `dotnet test` is not the test runner for this project. Report output directories must be new. HTML provides status labels, observed/expected values, reasons and sources, dataset identifiers, configuration and fingerprints. JSON retains the structured snapshot and configuration. No overall treatment-release decision is calculated.

CLI exits: 1 if any FAIL; otherwise 3 if any NOT VERIFIED; otherwise 0. REVIEW can still exist with exit 0; exit status is not treatment clearance. Exit 2 is input/output/runtime failure. The synthetic sample intentionally exits 3 because target levels and dose currency are not verified.

## Build and run in Eclipse — local verification required

On a licensed Windows development machine, use a Visual Studio Developer Command Prompt with the .NET Framework 4.8 developer pack and WPF/C# build tools. The ESAPI project preserves the supplied legacy format and configured RTM 18.2 reference path, overridable below. Compare the adapter members with the actual installed version and its API documentation.

```powershell
msbuild src\HSAdaptCheck.Esapi\HSAdaptCheck.Esapi.sln /p:Configuration=Release /p:Platform=x64 /p:EsapiAssemblyPath="C:\Program Files (x86)\Varian\RTM\18.2\esapi\API"
```

`EsapiAssemblyPath` must contain your licensed `VMS.TPS.Common.Model.API.dll` and `VMS.TPS.Common.Model.Types.dll`; these are not distributed here. The Windows project has its own solution and no package dependencies. The tested core source files are linked into this .NET Framework assembly, avoiding an additional runtime DLL and preserving separation of extraction from rules. Do not redistribute Varian assemblies or copy assemblies from a different Eclipse version. If the project format differs from the working local template, use that template's framework/reference conventions and add the adapter files plus linked core sources; do not enable write access.

Deploy `src/HSAdaptCheck.Esapi/bin/Release/HSAdaptCheck.esapi.dll` to a locally approved script folder using your site's ESAPI deployment/approval process. Script loading, dependencies and approvals must be tested locally. In Eclipse, open a nonclinical test patient and invoke the script. Select the reference and adaptive candidate across the open patient's courses, load independently completed configuration, then capture and check. No defaults select a plan. Configuration/selection changes invalidate the on-screen report. Export the HTML to an approved location. The script never calls `BeginModifications`, saves a patient or modifies a plan, structure, prescription, approval or treatment record.

The selected plans' extracted structure set/image UIDs are compared with independently recorded expected inputs; the CBCT is not guessed from the plan name. The tool does not prove that reference contours were transferred correctly, that a selected image is diagnostically adequate, or that generated planning products have correct anatomical provenance. Selectors use course/plan names only for navigation; identity checks use UIDs.

Reports are **snapshots**. The adapter cannot prove the absence of unsaved edits; follow the locally commissioned save/reopen procedure before capture. SHA-256 fingerprints identify the serialized input snapshot/configuration, not the entire Eclipse dataset, and are not signatures or tamper-proof attestations. Rerun after any relevant edit. Saving a report creates no validity guarantee for later plan states.

## Privacy and scope

No patient name or patient ID is deliberately extracted or logged. Reports include plan/course/image/structure identifiers and may contain identifying text in plan or structure names; treat them as clinical data. Do not commit local configurations, clinical snapshots or reports. Default generated paths `local/` and `reports/` are ignored. Runtime failures show error categories without raw API exception text. No network services, telemetry or HSAdapt generation logic are used by the checker.

Outside v0.1: ARIA workflow/signatures, scheduling, delivered-fraction reconciliation, Mobius integration, continuous monitoring, deformable dose accumulation, automatic clinical contour assessment and treatment-release interlocks. Mobius3D/FX and Portal Dosimetry/array measurements remain separate dosimetric/delivery QA.
