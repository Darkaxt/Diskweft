# Diskweft staged implementation and reconciliation

Updated: 2026-11-04. Authoritative requirements: [SPECIFICATION.md](SPECIFICATION.md).
Authorization: complete through S5, including final release and actual system
replacement (`already_authorized`). Pathweft integration is expressly excluded
by the user; its source/runtime/caller remain unchanged.

## Stage map and final reconciliation

| Stage | Status | Verified specified outcome | Requirement ownership |
| --- | --- | --- | --- |
| S0 - Fork, specification and baseline | COMPLETE | Public named fork, owned checkout/remotes, requirement map, controlled baseline and setup publication | ENV-01..04 |
| S1 - Identity and classic live scanning | COMPLETE | Real folder/UNC -> live tree/statistics -> stable native navigation -> correct terminal results, independent identities | ID-01; SCAN-01,03..07 for classic; affected COMPAT-01 |
| S2 - Progressive NTFS/MFT | COMPLETE | Actual elevated raw NTFS -> useful live folders/statistics -> native navigation -> terminal reconciliation; fallback/all-drives integrated | SCAN-02; MFT/mixed SCAN-03..07; affected COMPAT-01, VERIFY-01 |
| S3 - Windows appearance | COMPLETE | Actual native themes/accents/high contrast, persistence, Windows notifications and layout/DPI acceptance | UI-01..04 |
| S4 - Integrated verification and product delivery | COMPLETE | Compatibility/performance acceptance; corrected normal v0.1.1 independently delivered, including live order and Explorer colors | PERF-01, VERIFY-01, DELIVER-01, UI-05; integrated ID/SCAN/UI/COMPAT |
| S5 - Installed system replacement | COMPLETE | Installed 0.1.1 launch/classic/raw NTFS/history/routes verified; data/recovery preserved; SpaceScan uninstalled and obsolete registrations retired | MIGRATE-01 |

The original S0-S5 criteria are satisfied and verified. Maintenance below is
authorized by the reported recycle freeze and post-copy Clipboard.Flush failure.

| Maintenance stage | Status | Acceptance |
| --- | --- | --- |
| M1 - Responsive recycle and reliable Copy path | COMPLETE | FIX-01/02 verified: actual context-menu recycle/bin entry and parent reconciliation; dispatcher, cancel/error/busy/close safety; clipboard Unicode/persistence/contention; affected UI regressions and absent production hooks; installed/portable/uninstall packages |
| M2 - Verified maintenance delivery | ACTIVE | FIX-DELIVERY: publish/retrieve v0.1.2 and verify actual local update with existing data preserved |

The policy blocker is resolved through the user-installed Coordinator 0.20.4 and
updated skill 1.1.0 sandbox.powershell route. Actual receipts prove reviewed script
entry, STA/process-only RemoteSigned and no host/persistent policy change.
FIX-01/02 native recycle/permanent-delete/busy/close/error reconciliation and
clipboard contention/Unicode/persistence pass; actual bin entry is verified.
Classic navigation/restart/listing/history, live ordering and notes regressions
pass. Normal installed/portable CLI/native scan and uninstall pass. All M1
acceptance is satisfied; no blockers or tracked deferrals. M2's remaining criteria
are publication/retrieval and actual installed update verification. See
[M1 evidence](verification/M1-FILE-ACTIONS.md).
The [policy issue report](verification/M1-SANDBOX-POLICY-BLOCKER.md) is historical.

Execution order was S0 -> S1 -> S2 -> S3 -> S4 -> S5, with at most one ACTIVE
stage. Actual S5 first launch exposed a release-notes defect: S5 was parked,
S4 reopened for that correction plus explicit user live-order/color requirements,
then S5 resumed only after corrected release delivery passed. No incomplete feature
stage triggered publication. S4 owns product release; S5 owns the separate expressly
authorized installed-system rollout.

## Acceptance evidence

### S0 - Fork and baseline

ENV-01..04 PASS: public fork/parent verified through GitHub; maintained D: checkout,
origin/upstream/default target, controlled baseline, private references, specification/
design/development artifacts and cleanup verified. Setup commit
`1e493151734f93eda414031074aa590c5f83b023` was pushed and independently read back.
No production-code change or inherited GUI/installer launch counted as S0.
See [S0 evidence](verification/S0-BASELINE.md).

### S1 - Classic live scanning and identity

| Criteria | Verified complete workflows |
| --- | --- |
| ID-01 | Independent executable/metadata/icon/profile/history/registry/card/updater/installer; upstream and Pathweft sentinels preserved; normal build omits diagnostic actions. Package isolation reverified in S4. |
| SCAN-01,03 | Real growing ancestor totals, coalesced bounded publication, exact final fixture results and real UNC scan. |
| SCAN-04 | Stop/restart/failure, real denied-MFT fallback, stale listing rejection and worker shutdown. Stopped subtree cannot become complete history. |
| SCAN-05 | Native keyboard expansion, asynchronous listings, file/folder selection and virtualized scrolling survive updates/final relisting. Largest-first live-order acceptance completed in S4. |
| SCAN-06,07 | Partial extension/top-file publication, pending/inaccessible/stopped cues, discovered-byte shares and partial snapshot provenance. |
| COMPAT-01 affected paths | Exclusions, native long paths, real ACL denial/restoration, reparse exclusion, search/duplicate hashing, snapshots/compare/history, reports/CLI and busy destructive-key refusal. |

See [S1 evidence](verification/S1-CLASSIC-LIVE.md). Commit
`36e4f3a6d0ceac7758573344953d3b3f515f9cea` was pushed and independently read back.
No product release belongs to S1.

### S2 - Progressive MFT

SCAN-02, MFT/mixed SCAN-03..07 and affected COMPAT-01/VERIFY-01 PASS: actual
elevated raw NTFS -> live native folders/statistics -> expand/list/select ->
terminal result; real fallback, cancel/restart and separate-volume mixed roots.
Record regressions cover reordered parents, late exclusions, unresolved provenance,
extent correction and same-numbered records across volumes. Classic/UNC and normal
identity/CLI checks passed. Synthetic records supplemented real boundaries.
See [S2 evidence](verification/S2-PROGRESSIVE-MFT.md).

### S3 - Native appearance

UI-01..04 PASS: actual Settings, light/dark/system/custom accents/high contrast,
keyboard focus, Windows HWND notifications, restart persistence, measured layouts
and WPF DPI transforms. Classic navigation/scroll/history and normal CLI identity
passed. See [S3 evidence and limits](verification/S3-WINDOWS-APPEARANCE.md).
Owned guest exit/lease release verified; cleanup removed 711 members / 26,475,359
logical bytes without residuals (`29e0594183ec4d4e47dc96cb2ee3218e`).

### S4 - Integrated verification and delivery

PERF-01/VERIFY-01 and compatibility PASS. Three alternating paired runs after
warm-up: classic baseline/fork medians 781.7848 / 808.9633 ms (+3.4765%),
raw NTFS 1,863.1023 / 1,949.3709 ms (+4.6304%). Classic controls process-only CPU0
affinity on this hybrid host; initial unrestricted +26.3743% failed and remains
recorded. Peak memory/CPU/publication/backlog and boundaries are in
[S4 evidence](verification/S4-INTEGRATED-DELIVERY.md).

Current 0.1.1 real 1.19-million-file NTFS UI: useful metadata 115.612 ms, first
useful WPF 533.3272 ms from start / 417.7152 ms after publication; dispatcher p95
39.589 ms. Navigation, terminal reconciliation and zero workers passed. Actual raw
cancel/restart/fallback/mixed-root checks passed. Scanner code is unchanged by
0.1.1, so applicable paired scanner evidence remains valid.

SCAN-05/UI-05/startup correction PASS: both regressions reproduced red, then real
native notes/live largest-first ordering passed with file selection, expansion and
viewport anchor preserved. Actual Windows palette, dark readable caption, warmer
command/address surface, neutral selection and blue capacity bars verified without
layout redesign. Full native light/dark/custom accent/high contrast/layout/DPI/
notifications/persistence passed. DWM colors are Set-only: successful setters and
physical caption capture were used; unsupported Get was not claimed as readback.

Final owned guest `6d2c71af-8721-4315-8a62-acbe6915e8b2` passed current native and
normal installed/portable/uninstall workflows (exit0/isError=false). Aggregate guest
exit1 includes an earlier unsupported headless-DWM assertion; exact guest exit and
lease release were verified. No host fallback was used.

DELIVER-01 PASS: normal published
[v0.1.1](https://github.com/Darkaxt/Diskweft/releases/tag/v0.1.1), source/tag
`76dd613b514ef9ab32744609de32972407581560`, release434123183, installer/portable/
checksums present, draft=false/prerelease=false. Bridge read back state/assets/
digests; gh independently retrieved packages. Actual downloaded native/CLI,
version/hash/configuration/MIT/signature checks passed. Exact setup hash passed
normal guest installation/uninstall and S5 host install. Authenticode: NotSigned.
Initial 0.1.0 evidence is historical; corrected 0.1.1 is the supported release.

### S5 - Actual system replacement

MIGRATE-01 PASS. Fresh inventory preceded mutation; independently delivered 0.1.1
installed through broker consent. Actual installed EXE bare-path launch completed
its classic scan and closed normally. Actual-profile native classic/raw NTFS,
history lookup and legacy provenance passed. Binary/configuration/registry/shell
arguments/Start shortcut/card readback matches 0.1.1.

Original profile/history preserved; copied C: snapshot hash matches; all 37
pre-existing Diskweft histories match inventory. Recovery manifests, 44 files,
five registry exports, two shortcuts and original card verified. Recovered original
scanner and snapshot reader executed successfully. Full system reinstall was not
exercised; its exact restoration procedure and material remain retained.

SpaceScan's original native uninstaller succeeded; executable/install folder,
uninstall/executable registrations, three shell verbs and two shortcuts absent.
Obsolete card retired through transactional cleanup. Final read-only installed/
data/recovery/routes/cleanup reconciliation passed. Pathweft remains untouched
under explicit user exclusion, not a deferral.
See [S5 evidence and recovery boundaries](verification/S5-SYSTEM-REPLACEMENT.md).

## Cleanup and retained outputs

All spent scratch consumers exited and required cleanup transactions closed.
D: native-I/O scratch used the documented exception and was cleaned after use.
Inno's unavoidable C: staging was retired with the same reviewed ticket through
the administrator broker. No broad profile/Temp sweep. Versioned packages,
baseline, required private evidence and intentional G: recovery remain preserved.

| Final transaction | APPLIED result |
| --- | --- |
| `265f13bdd6ca51c127fcefbe951b05db` | 4,383 members / 2,457,915,709 logical bytes; detached optimization VHD/native scratch |
| `3b6761e6128271a91d1b5d06cf875929` | 6,645 members / 11,747,633 logical bytes; maintenance native/build/guest scratch |
| `0a50d850cf836dabecd22e7efad54f47` | 18 members / 4,492,290 logical bytes; obsolete generated candidates/card |
| `3798292eb2651109aca3092964e28e09` | 63 members / 6,547,082 logical bytes; independent delivery/fixture scratch |
| `a2820bb6468b4b867fa264a5b8363796` | Six members / 4,422,817 logical bytes; native staging/three owned fixture histories |

Earlier S4 million-fixture cleanup closed after late diagnostic residuals were
separately reviewed; S3/S4 evidence records original transactions. No required
operation or cleanup residual remains active. Do not replay completed migration,
installation, native uninstall or cleanup operations.

## Closure assessment

Final reconciliation covers every ENV/ID/SCAN/UI/COMPAT/PERF/VERIFY/DELIVER/MIGRATE
requirement and the complete observable design contract. No required blocker,
deferral or missing integration remains in authorized scope.

One scanner owns mutable state, one dispatcher owns projection; classic/CLI share
traversal, MFT parsing remains shared. Attempt generation owns listing/publication
lifetime; query cache invalidates at scan boundaries. Bounded raw reader joins
before handle close, without arbitrary lifecycle timeouts or broad disk fanout.
Appearance owns palette/native capabilities, without framework migration. Sorting
reuses stable rows; missing artwork uses text. Replacement uses native installer/
uninstaller and independent identities with original recovery, without a legacy
compatibility layer. No material consolidation finding requires another stage.
