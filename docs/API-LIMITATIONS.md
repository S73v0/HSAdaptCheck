# ESAPI integration boundary

Local .NET Framework: 4.8 (user confirmed). Target per user confirmation: Varian RTM 18.2 (the uploaded legacy project’s stale 16.1 path has been updated). Actual installed Eclipse/ESAPI binary versions: not inspected. No installed assemblies or runtime available on the cloud host. The uploaded scaffold, project and solution are source/build metadata, not compiled Varian assemblies. The host preserves the supplied legacy project/solution conventions and uses linked core sources for single-DLL deployment. Integration is unverified; the adapter is an implementation to commission, not an assertion of compatibility.

The public entry point directly uses `ScriptContext.Patient`, `Patient.Courses`, `Course.PlanSetups`, `ExternalPlanSetup`, `Course.Id` and `PlanSetup.Id`. Extraction uses guarded public member reads because the exact installed API version is unknown. It does not fabricate API stubs. Reflection avoids compile-time assumptions for optional members; it does **not** prove member semantics or runtime support. Both absent members and getter exceptions produce NOT VERIFIED extraction rows; failure of collection enumeration marks the collection incomplete.

Commission each mapping against the installed Varian documentation and independently inspected Eclipse data:

| Domain | Adapter members / semantics to confirm |
| --- | --- |
| Identity | PlanSetup.UID, Course.Id, StructureSet.UID, Image.UID, Image.FOR |
| Prescription | NumberOfFractions, DosePerFraction.Dose, DosePerFraction.Unit; only exact Gy/cGy supported, relative dose rejected |
| Structures | StructureSet.Structures; Structure.Id, IsEmpty, Volume (cc) |
| Calculation | Dose availability; GetCalculationModel(CalculationType.PhotonVolumeDose); Dose.XRes/YRes/ZRes in mm |
| Treatment | Beams; IsSetupField; TreatmentUnit.Id; EnergyModeDisplayName; Meterset.Value (MU); IsocenterPosition.x/y/z in DICOM mm |

No target-level prescription API linkage is implemented. No inference of multi-level target prescriptions from global Rx, optimization objectives, target names or dose statistics is made. Dose availability and lattice spacing do not establish dose currency. The adapter does not check MLC/gantry/control-point delivery parameters, collision clearance, imaging quality, anatomical contour validity, registration/transfer correctness or HSAdapt generation history. Identity matching establishes agreement with independently recorded UIDs, not a full audit trail. No explicit SCH/CRUDE plan comparison is implemented; the UI intentionally checks one reference and one adaptive candidate.

The ESAPI context may include unsaved edits. The checker does not save or prove a saved state; its saved-state provenance row is NOT VERIFIED. Follow a locally controlled save/reopen procedure before capture and commission that process independently.

All extraction is synchronous on the script/UI thread. No ESAPI objects are passed to background tasks. The script uses a modal WPF window and no database writes. UI responsiveness with large datasets and the site's scripting UI/thread model must be checked locally. Failed capture removes the active report; changing plan/configuration invalidates it. Editing Eclipse data after capture is not monitored. HTML export is an original snapshot, not a live report.

Review algorithm identifier/beam energy string formatting, setup-field filtering, frame UID access, dose units and coordinates particularly carefully on the installed release. A successful reflection call is not commissioning evidence. The pure snapshot runner trusts the supplied snapshot as data; JSON may be authored or edited and is not authenticated ESAPI evidence.
