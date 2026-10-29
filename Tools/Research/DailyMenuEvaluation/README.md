2026-10-30: Current owner is Dining. RunProduction.py expects input/Agriculture, input/Dining and input/DailyMenu; it loads the separated packages. It is manual-only; the separated run is documented in Docs/ExtractionValidation.md. The other probes and result files below are historical.

# Daily Menu feasibility evidence

**Production update (2026-10-29):** The loadable implementation is now in
Source/AMJAgriculture.Dining/ and Assemblies/AMJAgriculture.Dining.dll.
RunProduction.py and ProductionProbe.cs exercise actual Agriculture and MO
food through this production DLL. The proof below remains historical; see
[Dining](../../../Docs/Dining.md) and its bounded production evidence.


This folder holds AMJ-authored disposable native probes, not a released Mod.
The decision and exact scope are in
[`DailyMenuNativeAssessment-20261028.md`](../../../Docs/Research/DailyMenuNativeAssessment-20261028.md).

`Probe.cs` drives a generated 50x50 Quickstarts map through real Daily Menu food
jobs using Pickle. `PlatePrototype.cs` adds test-only role selection and bundles
the real dispensed Things before the existing job queues ingestion. Its Core
meal role mapping, lack of interrupted-job recovery, and limited plate lifecycle
are deliberate feasibility limits, not production defaults.

`run.py` records the one-shot orchestration used on the private WSL host:
copy only the required launcher files, reference immutable game Data/Managed,
use a fresh isolated Mods and savedatafolder, compile the AMJ probe, repair only
the copied vendor Supply helper, and run with Xvfb. It has explicit host/input
paths and refuses an already-existing run directory. The host has automount
disabled; the actual inputs were transferred to `/tmp/amj-daily-input-20261028`,
and the runner's input paths were adjusted to that directory. The final run
directory was `/tmp/amj-daily-menu-20261028-r6`. Do not point it at live Steam
Mods or reuse a live savedatafolder. No automatic workflow invokes this script.

The original Workshop package, decompiled vendor code, licensed game files and
repaired vendor DLL are **not included** here. Supply is compiled from the
locally acquired vendor-provided Source/Supply.cs, with its helper renamed to
avoid Harmony's reserved `Prepare` callback name. This is a provider correction,
not evidence of an entirely unmodified-provider compatibility patch.

Evidence JSON is an extract; the complete Unity/launcher logs remain in the
task's local `Mods/DailyMenuEvaluation/Evidence` directory. `Original.json` has a
passing behavior scenario but startup errors, so overall acceptance FAIL.
`RejectedGuard.json` has exit139 and no scenario: an empty extracted error array
does not make it PASS. `Patched.json` has exit0, one passed native scenario,
zero extracted errors, and both baseline/plate completion markers. Source and
probe hashes and the single changed vendor file were added from the final
isolated run after exit. No claim covers MO integration, old saves, 1.7, UI art,
full audio/Steam initialization, production food classifications or balance.
