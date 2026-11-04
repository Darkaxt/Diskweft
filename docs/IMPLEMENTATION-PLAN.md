# Diskweft implementation plan and reconciliation

Authoritative source: [SPECIFICATION.md](SPECIFICATION.md), version 0.4.
Updated: 2026-11-04. Current authorization: complete through S5,
"You may start working on this all the way in" (`already_authorized`).

## Stage map

| Stage | Status | Complete specified outcome | Requirement ownership |
| --- | --- | --- | --- |
| S0 - Fork, specification and reproducible baseline | COMPLETE | Public named fork, owned checkout, complete requirements/stage map, inspected baseline build/scanner evidence and published setup documents | ENV-01..04 |
| S1 - Independent identity and classic live scanning | COMPLETE | Real folder/UNC scan -> consistent live tree/statistics -> navigation -> correct terminal result, with independent Diskweft identities | ID-01; SCAN-01,03..07 for classic; COMPAT-01 for affected classic paths |
| S2 - Progressive NTFS/MFT scanning | COMPLETE | Real volume -> progressive resolved folders/statistics -> navigation -> final result; safe fallback and all-drives integration | SCAN-02; SCAN-03..07 for MFT/mixed scans; affected COMPAT-01, VERIFY-01 |
| S3 - Windows 11 appearance and accents | NOT STARTED | Native light/dark/high-contrast UI, Appearance persistence and live accent/theme changes across completed scan workflows | UI-01..04 |
| S4 - Integrated verification and final delivery | NOT STARTED | Full specification reconciliation, compatibility/performance evidence, verified final commit and independently retrieved normal release | PERF-01, VERIFY-01, DELIVER-01; integrated ID/SCAN/UI/COMPAT acceptance |
| S5 - Replace installed SpaceScan with Diskweft | NOT STARTED | Verified Diskweft installation and preserved data, existing SpaceScan launch/integration routes replaced, rollback proved and obsolete owned registrations retired | MIGRATE-01 |

Only one stage may be ACTIVE. S1 depends on S0; S2 on S1; S3 on S2; S4 on S3.
S5 depends on the verified S4 release and a fresh installation/consumer inventory.
No release follows an individual feature stage. S0's docs/source publication is
setup delivery, not a Diskweft product release. Execution through S5 is authorized;
advance sequentially only after the current stage meets its acceptance criteria.

## S0 acceptance

| Criterion | Required evidence | Current result |
| --- | --- | --- |
| ENV-01 | GitHub fork visibility, parent and preserved upstream source commit | Fork created; bridge/gh read-back confirms public fork and parent |
| ENV-02 | Local checkout/remotes/default GitHub target; committed setup and matching remote HEAD | PASS: setup commit 1e493151734f93eda414031074aa590c5f83b023 independently read back through GitHub bridge and git ls-remote; tracked worktree clean |
| ENV-03 | Existing upstream build, actual version/hash, controlled scanner totals/exclusion/stats/pre-cancelled results; no production-code changes | PASS: existing build and controlled checks exited 0; source unchanged; see verification/S0-BASELINE.md |
| ENV-04 | Complete mapping, design/verification contract, development notes, actual evidence, private reference handling and scratch cleanup | PASS: committed/published documents and helpers read back; all requirements assigned; private reference ignored; fixture/preflight cleanup applied |

S0 reconciliation: ENV-01..04 satisfied and verified. No remaining S0 acceptance
criteria, blockers or tracked deferrals. S0-S2 are COMPLETE; no stage is ACTIVE.
S3-S5 remain NOT STARTED.
Evidence: [S0 baseline](verification/S0-BASELINE.md).

## S1 current reconciliation

| Criterion | S1 result and evidence |
| --- | --- |
| ID-01 | PASS: independent executable/icon/profile/registry/card/updater and installer-source identity. Isolated upstream/Pathweft sentinels and actual upstream bindings preserved; normal build omits diagnostic actions. Installer execution/package acceptance remains S4. |
| SCAN-01,03 | PASS: real filesystem partial ancestor totals, coalesced publication and bounded 2,100-file case; exact controlled final results and real UNC scan. |
| SCAN-04 | PASS: native Stop/restart, failure terminal state, real denied-MFT fallback, late old-generation listing rejection and owned-thread shutdown. Subtree completion cannot promote a stopped tree to complete history. |
| SCAN-05 | PASS: native keyboard expansion, async listing, directory/file selection and real virtualized scrolling persist through updates and final relisting. |
| SCAN-06,07 | PASS: partial extension/top-file publication, pending/inaccessible/stopped cues, discovered-byte shares and partial snapshot provenance. |
| affected COMPAT-01 | PASS: name/path exclusions, native long paths, real fixture ACL denial/restoration, existing reparse exclusion, search, duplicate hashing, snapshots/compare/history, CSV/HTML/CLI and busy destructive-key refusal. |

Current evidence: [S1 verification](verification/S1-CLASSIC-LIVE.md). Every assigned
criterion is satisfied. Source/evidence commit
`36e4f3a6d0ceac7758573344953d3b3f515f9cea` was pushed and independently read back
through the GitHub bridge, with matching remote HEAD and key source/document blobs.
S1 is COMPLETE with no remaining acceptance criteria, blockers or tracked
deferrals. No product release belongs to S1. The system-replacement stage is
documented, not started.

## Later-stage acceptance and verification

S2 reconciliation: SCAN-02 and the MFT/mixed portions of SCAN-03..07, affected
COMPAT-01 and current-stage VERIFY-01 passed. Actual elevated raw NTFS -> live
native folders/statistics -> expand/list/select -> terminal result, fallback,
stop/restart and separate-volume mixed roots were verified. Current-source raw
record regressions cover ordering, late exclusions, unresolved provenance,
extent correction and same-numbered records across volumes. Classic/UNC and
normal-build identity/CLI regression passed. See
[S2 verification](verification/S2-PROGRESSIVE-MFT.md). No remaining S2 acceptance
criteria, blockers or tracked deferrals. No S3 work has begun.

### S1 - Independent identity and classic live scanning

- Establish all identity boundaries before normal runtime trials. Inspect updater,
  installer, shell/registry, profile/history and ecosystem-card ownership.
- Implement the complete classic workflow: coalesced publications, parent totals,
  live tree/stats, asynchronous navigation, stable rows and terminal reconciliation.
- Cover folder, UNC/slow path, all-drives classic children and partial rescans.
  Preserve existing file actions' safety and partial-history provenance.
- Verify actual native actions while scanning, controlled exact final totals,
  exclusions/reparse/long paths, cancellation, rapid restart and isolated profiles.
- Reconcile every assigned SCAN/COMPAT criterion. Progressive MFT work remains S2;
  S1 retains only per-directory extension metadata from existing MFT finalization
  to seed correct subtree-rescan statistics. This does not satisfy SCAN-02.

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

### S5 - Replace installed SpaceScan with Diskweft

- Inventory the installed application/version, settings/history and all actual
  launch/integration consumers. Include Pathweft's scan route when present.
- Establish a concrete rollback record; preserve original user data and compare
  hashes/conversion results before switching routes.
- Install the independently verified S4 package, migrate applicable settings and
  history, then switch owned shortcuts/shell/caller registrations to Diskweft.
- Verify real installed launches, caller arguments, classic/MFT operation and
  history access; prove rollback before retiring obsolete SpaceScan registrations
  or installation files. Use broker consent for administrator changes and the
  proper reviewed cleanup path for explicitly retired generated/install outputs.
- Do not perform S5 during S1. A published release alone cannot close MIGRATE-01.

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

S1 closure assessment: publication/scan state is owned by `ScanSession`, display
projection and asynchronous listing lifetime by `ClassicUI`; CLI and GUI share
the classic walk. No duplicate scanner or speculative framework was introduced.
The query directory cache is invalidated at each new scan generation. No material
required consolidation remains before S2; full MFT streaming/theming/performance
and package acceptance stay in their existing assigned stages.

S2 closure assessment: raw parsing/runlist code remains shared, the obsolete
final-only scan import path was removed, and scanner/dispatcher ownership remains
explicit. Pending exclusion names are reread from compact record references;
failed attempts have one explicit replacement transition. No material required
consolidation obstructs S3. S4 still owns complete performance qualification and
final package verification; no intermediate product release was created.
