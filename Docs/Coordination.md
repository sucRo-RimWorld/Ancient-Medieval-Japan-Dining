# Current handoff

2026-10-31: Owner-approved Daily Menu dependency removal. Dining now owns serving-table storage, portions, meal records and doctor/warden meal substitution. Harmony only; Culture/Agriculture/MO are not Dining dependencies. Agriculture still owns food/role mapping.

Current owner spec: [Design](Design.md). Graphical meal layout, main/side/garnish separation and numerical rebalance remain later work. The old source and DLL hashes in Migration.json are extraction history. Current acceptance runner: Tools/Acceptance/RunIndependent.py, with no Daily Menu package. Do not use historical provider-dependent probes as the current entry point.

Current dependency-removal runtime evidence: [2026-10-31 acceptance](IndependentValidation.md).
