# Validation record

Development host: Linux; .NET SDK 8.0.426 installed from the official Microsoft artifact, SHA-512 checked against official release metadata. No Varian binaries, Eclipse runtime or Windows WPF runtime available. Target provided by user: Varian RTM 18.2, .NET Framework 4.8, x64. The supplied legacy project/solution and script scaffold were inspected and adapted; stale 16.1 reference paths were updated to 18.2.

## Executed successfully

- Frozen NuGet restore of the portable solution using committed lockfiles.
- Release build of .NET Standard 2.0 core, .NET 8 CLI and test executable: zero warnings/errors.
- 28 synthetic tests executed, 28 passed, zero failed. Cases cover UID mismatch despite plausible name, same-plan selection, missing/empty/ambiguous structures and overlapping mappings, separate original/adaptive Rx mismatch, absent expectations/confirmation, unsupported/mismatched algorithm, missing dose and MU, machine/energy/MU/isocentre mismatch, added/zero beams and incomplete extraction, tolerance boundaries/invalid data, target-level limitations, visible extraction failures, report HTML escaping, JSON roundtrip and BOM handling, input fingerprints and rationale that cannot override a discrepancy.
- Functional CLI report export of a synthetic snapshot to HTML and JSON. Parsed current output files and verified identifiers, report contents and explicit NOT VERIFIED labels. Exit 3 as documented; unavailable dose currency, saved-state and target-level evidence were not converted to PASS.
- Unconfigured example: exit 3, no PASS rows.
- Wrong CBCT UID: exit 1 and the affected identity check FAIL.
- Existing output path and missing input: exit 2; missing input creates no output report.
- Reusable cloud install script executed: frozen restore, Release build and all tests pass; repeated invocation succeeds with retained SDK/dependencies.

The test executable is the assertion runner. A zero-test `dotnet test` invocation is not validation for this repository. CI is configured to run the executable but GitHub-hosted CI results have not been observed here.

## Not tested / not established

No ESAPI compilation against licensed 18.2 references, Eclipse launch, WPF UI operation, actual clinical data extraction, local field/unit semantics, deployed script approval or read-only behavior in Eclipse has been verified. The reference path is a configuration target, not evidence of installed binary compatibility. No clinical thresholds or machine/energy defaults have been invented or commissioned. No local clinical/commissioning dataset was used.

Target-level prescription linkage, dose currency and saved-state provenance remain NOT VERIFIED. Clinical anatomy, full provenance/transfer correctness, full beam delivery parameters and all out-of-scope workflows remain unverified. The cloud workflow validates independent software logic and synthetic reporting only. Independent physicist review, local commissioning and clinical approval are still required.
