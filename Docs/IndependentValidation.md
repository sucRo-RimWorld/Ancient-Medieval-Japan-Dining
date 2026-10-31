# Daily Menu independent acceptance — 2026-10-31 JST

## Result and scope

PASS for the dependency-removal scenario on RimWorld **1.6.4871 rev600** in the isolated AMJRuntime Linux/Xvfb environment. Pickle: **1 passed / 0 failed / 0 skipped / 0 flaky**, process exit 0, complete captured game log **0 errors**. Daily Menu was absent both from loaded assemblies and Dining assembly references. The shipped DLL was tested without rebuilding it inside the runner.

DLL SHA-256: `8a856658446953b27089554f07fe7d0772783b1958d4c30e4b69ac38334558ec`.

This is an integration scenario with Agriculture and MO, not a runtime test of Dining alone. Framework/test dependencies are recorded in [ModsConfig.xml](ValidationEvidence/Independent-20261031/ModsConfig.xml). The host's existing audio repair belongs to Project #1149; this change does not modify host audio or the player's configuration/save.

## Observed behavior

- Actual cooking bills produce Agriculture meals; vanilla hauling fills three independently selected serving tables.
- Fractional dispensing conserves nutrition, keeps rice ingredients and respects food restrictions.
- Normal hunger AI assembles and eats one tray, records the three foods without counting the tray, and applies the MO soup effect.
- Interrupted collection, partial eating, tray Scribe roundtrip and subsequent eating preserve food/nutrition.
- Vanilla doctor workgiver eligibility leads to an actual patient-feeding job using the tray.
- Missing optional dishes permits a staple-only tray; Agriculture's simple-meal bill removal remains effective.
- A synthetic old-name buffet with old saved fields roundtrips and releases its stored meals into ordinary storage.

The first trial exposed an incorrect shelf graphic class and an incapable randomly generated cook in the fixture. The graphic was corrected to vanilla Graphic_Multi and the fixture now selects a capable cook. The successful result below is the corrected run.

## Evidence and reproduction

- [Machine result and assertion markers](ValidationEvidence/Independent-20261031/result.json)
- [Pickle summary](ValidationEvidence/Independent-20261031/report-summary.json)
- [Complete game log, gzip](ValidationEvidence/Independent-20261031/unity.log.gz)
- [Launcher log, gzip](ValidationEvidence/Independent-20261031/launcher.log.gz)
- Current runner: `Tools/Acceptance/RunIndependent.py`; scenario: `Tools/Acceptance/IndependentDining.cs`.
- Static checks: `Tests/validate_migration.py --agriculture ../AncientMedievalJapanGrains` PASS; `Tests/Validate-Assembly.ps1` PASS (references only mscorlib, Assembly-CSharp, 0Harmony, System.Core, UnityEngine.CoreModule); XML parsing and git diff whitespace checks PASS.

## Explicit limits

No real player old save, previous-provider tray fixture, complete Daily Menu save migration, standalone Dining runtime, exhaustive warden/prisoner states, RimWorld 1.7 or new graphical layout was tested. The legacy-table test is synthetic and does not establish full old-save compatibility. Numerical rebalance and expanded roles are later work.

Agriculture's broad `Tests/validate_grains_base.py` currently fails on its unchanged main baseline (7c5a92d), starting with an unterminated string at line 156. Exploratory fixes were reverted; that broad suite is not reported as passing. Agriculture changes in this task only update dependency documentation/metadata; the paired targeted checks and actual integration scenario above passed. No unrelated food/test repair is included.
