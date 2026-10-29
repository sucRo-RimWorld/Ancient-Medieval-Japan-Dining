# AMJ Dining

Read `Docs/Design.md`, `Docs/Coordination.md`, and Project's current `AGENTS.md` / `Docs/SharedRules.md` before work. Use topic branches and PR integration with exact-head substantive review as required by Project `Docs/GitHubMainProtection.md`.

Dining owns role-based meal assembly, serving, ingestion and meal UI. Culture is a category, never an implied dependency. Agriculture owns foods, recipes and their role assignments. Do not embed Agriculture/MO requirements in Dining.

This extraction preserves the legacy assembly/type/Def identities deliberately. Only Dining distributes AMJAgriculture.Dining.dll. Daily Menu remains required until an explicitly implemented replacement; do not redistribute its decompiled code. Build using `Scripts/Build.ps1`; validate the paired migration with `python Tests/validate_migration.py --agriculture PATH`. No automatic game launches or Steam publication. Existing historical runtime evidence does not prove the extracted pair has been game-tested.
