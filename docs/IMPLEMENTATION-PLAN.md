# Diskweft implementation plan and reconciliation

Authoritative source: [SPECIFICATION.md](SPECIFICATION.md), version 0.1.
Updated: 2026-10-30. Current authorization: specification and S0 only.

## Stage map

| Stage | Status | Complete specified outcome | Requirement ownership |
| --- | --- | --- | --- |
| S0 - Fork, specification and reproducible baseline | COMPLETE | Public named fork, owned checkout, complete requirements/stage map, inspected baseline build/scanner evidence and published setup documents | ENV-01..04 |
| S1 - Independent identity and classic live scanning | NOT STARTED | Real folder/UNC scan -> consistent live tree/statistics -> navigation -> correct terminal result, with independent Diskweft identities | ID-01; SCAN-01,03..07 for classic; COMPAT-01 for affected classic paths |
| S2 - Progressive NTFS/MFT scanning | NOT STARTED | Real volume -> progressive resolved folders/statistics -> navigation -> final result; safe fallback and all-drives integration | SCAN-02; SCAN-03..07 for MFT/mixed scans; affected COMPAT-01, VERIFY-01 |
| S3 - Windows 11 appearance and accents | NOT STARTED | Native light/dark/high-contrast UI, Appearance persistence and live accent/theme changes across completed scan workflows | UI-01..04 |
| S4 - Integrated verification and final delivery | NOT STARTED | Full specification reconciliation, compatibility/performance evidence, verified final commit and independently retrieved normal release | PERF-01, VERIFY-01, DELIVER-01; integrated ID/SCAN/UI/COMPAT acceptance |

Only one stage may be ACTIVE. S1 depends on S0; S2 on S1; S3 on S2; S4 on S3.
No release follows an individual feature stage. S0's docs/source publication is
setup delivery, not a Diskweft product release. No later-stage implementation
may begin under the current S0-only instruction.

## S0 acceptance

| Criterion | Required evidence | Current result |
| --- | --- | --- |
| ENV-01 | GitHub fork visibility, parent and preserved upstream source commit | Fork created; bridge/gh read-back confirms public fork and parent |
| ENV-02 | Local checkout/remotes/default GitHub target; committed setup and matching remote HEAD | PASS: setup commit 1e493151734f93eda414031074aa590c5f83b023 independently read back through GitHub bridge and git ls-remote; tracked worktree clean |
| ENV-03 | Existing upstream build, actual version/hash, controlled scanner totals/exclusion/stats/pre-cancelled results; no production-code changes | PASS: existing build and controlled checks exited 0; source unchanged; see verification/S0-BASELINE.md |
| ENV-04 | Complete mapping, design/verification contract, development notes, actual evidence, private reference handling and scratch cleanup | PASS: committed/published documents and helpers read back; all requirements assigned; private reference ignored; fixture/preflight cleanup applied |

S0 reconciliation: ENV-01..04 satisfied and verified. No remaining S0 acceptance
criteria, blockers or tracked deferrals. Only S0 is COMPLETE; no stage is ACTIVE.
S1-S4 remain NOT STARTED under the user's explicit current execution boundary.
The next action, when requested, is to start S1; no product implementation or
release is claimed by S0. Evidence: [S0 baseline](verification/S0-BASELINE.md).

## Later-stage acceptance and verification

### S1 - Independent identity and classic live scanning

- Establish all identity boundaries before normal runtime trials. Inspect updater,
  installer, shell/registry, profile/history and ecosystem-card ownership.
- Implement the complete classic workflow: coalesced publications, parent totals,
  live tree/stats, asynchronous navigation, stable rows and terminal reconciliation.
- Cover folder, UNC/slow path, all-drives classic children and partial rescans.
  Preserve existing file actions' safety and partial-history provenance.
- Verify actual native actions while scanning, controlled exact final totals,
  exclusions/reparse/long paths, cancellation, rapid restart and isolated profiles.
- Reconcile every assigned SCAN/COMPAT criterion. MFT remains unchanged until S2;
  that scheduled ownership does not satisfy SCAN-02 or overall completion.

### S2 - Progressive NTFS/MFT scanning

- Keep raw parsing/runlist correctness; add resolved/pending directory accounting,
  safe late exclusions and bounded publications during MFT reading.
- Integrate live tree/stats/navigation and attempt-specific fallback in individual
  and all-drives scans. Reconcile terminal totals without duplicate contributions.
- Verify reordered parent records and exclusions with reproducible tests, then
  real elevated NTFS, cancellation and genuine fallback. Synthetic records alone
  cannot close the real-volume criterion.
- Preserve classic acceptance; inspect actual capability/privilege results through
  the required broker. Performance diagnostics inform implementation; S4 owns
  the complete paired large-workload acceptance.

### S3 - Windows 11 appearance and accents

- Apply the specification's Explorer/Windows contract to the actual WPF shell,
  data views, menus, dialogs and Appearance settings without framework migration.
- Default to system theme/accent; persist System/Light/Dark and custom accent
  overrides. Handle live Windows preference changes, focus and high contrast.
- Use Huashu for hierarchy/readability and scan-specific state presentation.
  The user supplied the direction; generic three-option approval ceremony does
  not add an execution gate. No HTML demo is required for this native product.
- Inspect measured native layouts/screenshots at specified DIPs/scales and states,
  actual Windows notification handling and settings persistence after restart.

### S4 - Integrated verification and final delivery

- Close compatibility for CLI/reports, history/compare, search/duplicates, file
  actions and side-by-side identity; reuse unchanged applicable stage evidence.
- Execute real-path coverage and paired large classic/MFT performance measurements
  against retained baseline. Close latency/memory/backlog and scan-time criteria.
- Review relevant upstream drift, exact package contents, licenses, updater route
  and install/uninstall recovery. No unresolved required blockers/deferrals.
- Commit verified integrated code; publish normal v0.1.0 with installer, portable
  ZIP, checksums and truthful notes on Darkaxt/Diskweft. Independently retrieve and
  verify package version/hash/signature status, identities and packaged behavior.
- Record actual source/tag/assets/publication state and cleanup receipts. A green
  build, source push, draft release or CI artifact does not close delivery.

## Gap and deferral ledger

No required work has been moved out of its assigned stage. No tracked deferrals.
Future stages are planned ownership, not completion claims or unnamed deferrals.
If a required item must move, record its exact criterion, named destination,
current-stage independence and final verification here before proceeding.

Every blocker records criterion, concrete reason, internal/external cause,
resolution condition and dependent work. An implementable acceptance gap keeps
the stage ACTIVE; BLOCKED is reserved for a justified parked impediment.

## Closure assessment

S0's verified scope adds documentation and controlled baseline helpers, reusing
the existing build. No production code changed and no material consolidation
finding obstructs S1. The inherited identity/ownership problems are already
assigned S1 requirements, not justification for speculative S0 refactoring.
At later stage closure, inspect only implemented scope for evidence-backed debt
obstructing required integration and record findings here.
