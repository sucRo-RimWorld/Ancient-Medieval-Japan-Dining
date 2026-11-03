# AMJ Dining — agent instructions (new design from 2026-11-01)

## Start here
1. Read this file and `main:Docs/Coordination.md`, then the current `Docs/Design.md`.
2. Read Project `AGENTS.md`, `Docs/Coordination.md`, `Docs/SharedRules.md` and the applicable PR protection rules.
3. Work through topic branches and PRs; substantive exact-head PR review before merge.

## New author decision (supersedes ALL prior Dining design)
`Docs/Design.md` is the **new, author-decided single-item meal proposal**. The old tray/serving-table/multi-prepared-dish assembly approach and its design decisions are **cancelled**, including its planned role expansions and graphical layout. Do not reintroduce prior Dining behaviors on the assumption they are still accepted.

**Critical separation:** current C#/Defs/DLL/About/README historical descriptions and 2026-10-31 runtime evidence still describe the **legacy implementation, not the new design**. Do not claim the new design is implemented or tested. Preserve historical evidence until migration decisions are authorized; do not ship obsolete runtime as if it implements the new plan.

Current new proposal assigns food/ingredient role classification, all five recipe Bills, ingredient selection and meal product behavior to **Agriculture**. Whether there remains an independent Dining runtime Mod is **undecided**; do not invent a required Dining dependency or recycle the legacy DLL by default. Coordination with Agriculture/Project must use the relevant owner `main:Docs/Coordination.md`, never the user as courier. Historical dependencies (Daily Menu, MO, Harmony, etc.) do not determine new-plan dependencies.

Do **not** implement C#/XML, remove legacy artifacts, change package IDs, modify saves or launch tests from a design-only request. Preserve historical source for controlled retirement when the new design and migration are ready.

**Design decision recording:** When the author **confirms/adopts a design decision**, reflect it in the owning canonical `Docs/Design.md` through the normal topic-branch/PR/review/merge process without demanding a second 「反映して」 instruction. **Do not write unconfirmed proposals** to the canonical design: an assistant suggestion, an exploratory discussion, an unanswered question or a numerical example is not author confirmation. If whether a decision is confirmed is genuinely ambiguous, keep it in chat and ask instead of unilaterally finalizing it. Keep confirmed scope, open questions and unimplemented runtime clearly distinguished; never use this rule to implement code or alter unrelated design.

For material source work, check the applicable existing-Mod audit gate; use minimal justified verification, no automatic native launches or Steam publishing.
