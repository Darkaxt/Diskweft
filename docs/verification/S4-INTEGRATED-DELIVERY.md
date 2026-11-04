# S4 integrated verification and delivery

2026-11-04. S0-S3 COMPLETE; S4 ACTIVE; S5 NOT STARTED.
Pre-release product verification passes. DELIVER-01 still requires verified
commit/tag, normal publication and independent delivered-asset retrieval.

## Current acceptance evidence

Baseline: unchanged SpaceScan 1.4.1, source `313271a`, binary SHA-256
`1eacba02c172a76d21e7b0bed77da51783d04cfe1b0926dd2d5e51ef7e650def`.
Only scanner methods are invoked; its inherited GUI/installer is never launched.
Host: Windows 11 Pro build 26200, i9-13900HX, 24 cores/32 logical processors,
approximately 64 GiB RAM. Fresh processes/cold JIT; one filesystem-cache warm-up
per engine followed by three alternating paired runs. No cache flush or OS change.

| Requirement | Current result |
| --- | --- |
| PERF-01 classic | Actual 1,000,000 files / 1,011 directories / 747,520,000 bytes in task-owned dynamic 8 GiB NTFS VHDX backed by D:. 90% empty, 9% 1 KiB, 1% 64 KiB, equal bin/txt. Exact baseline/fork totals every run. CPU0 affinity applies identically to short-lived probes only. Median 781.7848 / 808.9633 ms: +3.4765%; median paired +3.4765%, PASS. |
| PERF-01 NTFS | Actual raw E:, approximately 1.19 million files / 104,600 directories / 872 GB logical bytes. Median 1,863.1023 / 1,949.3709 ms: +4.6304%; paired median +4.6304%, PASS. Ordinary inherited scheduling. Live-volume drift and unresolved/incomplete provenance reported honestly. |
| SCAN-03/06 bounds | Unconsumed classic mailbox <=1,011 directories / 1,000 largest files; MFT <=104,600 / 1,000. No per-file event history. Raw pipeline: two fixed 4 MiB buffers, one pending chunk, exactly one serialized disk reader. |
| Native million-file latency | Current owned host WPF window/read-only fixture: useful producer metadata at31.3406 ms; WPF folders557.4034 ms later /588.744 ms total. Dispatcher p95 12.3885 ms, max76.8715 ms, 160 samples; real navigation, exact totals, zero workers at idle. One outstanding diagnostic action/10 ms cadence; dispatcher boundary, not input-to-photon. |
| VERIFY-01 real MFT | Current real raw E: scan, partial folders, navigation, cancellation/restart, fallback and mixed roots passed. Useful partial boundary487.1177 ms. Broker request `9842ce6358bd4b928344517ef63b634d`, elevated=true, native exit0. |
| COMPAT-01 query | Actual Mft.Collect resolved the newly flushed owned137-byte marker at its exact path once, exercising the changed raw pipeline's query path. Same broker request, exit0. |
| Classic compatibility | Current controlled partial ancestors, cancellation/drain, totals, exclusions, long paths, actual ACL denial/restoration, accessible read-only UNC, denied raw fallback, mixed-source extensions, search, duplicate hashing, history/compare and updater identity passed. |
| MFT parser/accounting | Current reordered raw-record regression passed, including late exclusions, unresolved provenance, extents and cross-volume record identity. Supplements real-volume checks. |
| Normal build | 0.1.0; exact baseline CLI CSV, branded HTML, independent profiles/cards/registry/updater, production diagnostic hooks absent. Existing SpaceScan settings/routes unchanged. Exit0. |
| Native classic regression | Current guest keyboard expansion, selection, virtualized2,100-file scroll, stop/restart, stale listing rejection, subtree rescan/history, busy delete refusal and thread-aware close passed. Partial display453.2682 ms. |
| Current packages | Actual installer/portable CLI and normal WPF scans reached1file/1,234bytes. Installed shell route, uninstall, SpaceScan sentinels and owned process exit passed. Guest command exit0/isError=false. |
| UI-01..04 | Unchanged S3 appearance/accent/contrast/persistence/native notifications/layout evidence remains applicable; changed projection received current native regression above. |

Classic pairs (baseline/fork ms): 858.9296/847.7305, 693.6853/789.5673,
781.7848/808.9633; individual increases -1.3038%, +13.8221%, +3.4765%.
Baseline CPU703.125-843.750 ms; fork781.250-859.375 ms.
Peak working set30,875,648-31,043,584 versus27,869,184-28,090,368 bytes.
Publication4.6302-8.1566 ms/run. Both specified medians pass.

MFT pairs: 1,891.6224/1,927.7023, 1,863.1023/1,949.3709,
1,846.8786/2,081.9996 ms; increases +1.9074%, +4.6304%, +12.7307%.
Baseline CPU328.125-734.375 ms; fork1,093.750-1,593.750 ms.
Peak working set103,952,384-103,989,248 versus246,079,488-249,888,768 bytes.
Publication202.7165-258.8708 ms; four bounded final filename rereads.
Raw-read/parsing durations overlap; do not sum them as sequential phases.
CPU deltas include setup; wall timing brackets scanning. PeakPagedMemorySize64
is labelled peak paged bytes, not private working set.

## Diagnosis and implementation

Initial MFT overhead was +433.7625%, including approximately5.7seconds publishing.
The cohesive correction caches stable paths, copies direct-folder extension
detail only at terminal handoff, aggregates extension deltas, reuses bounded top
names, removes a per-record closure allocation and overlaps sequential reads with
parsing. Ancestor flushing measured26-44 ms; no speculative rewrite was made.

The parser remains sole owner of records/tree/statistics. The shared raw-handle
lock serializes chunk reads and rare record rereads. Iterator disposal cancels
bounded internal waits and joins the actual reader before handle close.
Cancellation/fallback/query/restart passed at real boundaries. First useful
publication is immediate; subsequent cadence500 ms, terminal event-driven.

The first new-VHD unrestricted classic set missed (+26.3743%). Core/live
diagnostics showed significant variation without a production change. CPU0
controls placement on this hybrid host; this does not prove all earlier variation
came from core type. Failed and successful raw results remain private. Earlier
ordinary-D: classic passed +4.2851%/paired+6.3500%; current acceptance uses the
explicit VHD/CPU0 result above.

Upstream drift reviewed through `a33675a` (1.4.4). Adopted the relevant updater
original-user launch correction; Setup keeps runasoriginaluser and /NOLAUNCH for
managed installation. No unrelated donation UI. Interactive updater token changes
are not claimed verified by the SYSTEM guest; actual installed launch belongs S5.

## Packages and pending delivery

Version0.1.0; Inno Setup6.7.4; MIT included; Authenticode NotSigned.
Current output `dist/0.1.0`; earlier candidates superseded and unpublished.

| Item | SHA-256 |
| --- | --- |
| Diskweft.exe | `1e3875d18bd55c93cf554ef4a761fcbed0f067ba7f8fb75362e6ea8cc7a5346b` |
| Installer | `8102e74d930d78e5f4e56056edca625c2cc2a9cf7f82d4ac0b141ca5cf63e7db` |
| Portable ZIP | `569a6314fbd3998f84bd60d7c93596bee18dce1ad45919893338ff41613e0bef` |
| DPI config | `28472e5da425eccd7d67a8d830d94524e8bf08ef321991731dbdda2d07a1bc75` |

Current4 GiB/no-network owned guest `c6bb37ee-0a40-4594-a517-d2443896fac6`;
request `s4-current-native-and-packages`, native exit0/isError=false.
Workflow ended with exit0, sandbox_exit_verified=true and lease released.
Normal installed/portable executable and config hashes match the table exactly.

DELIVER-01 is an internal advancement blocker until verified source is committed,
normal v0.1.0 published with installer/portable/checksums, then independently
retrieved and checked against tag/source/version/hash/signature/configuration
and usable behavior. S5 remains NOT STARTED behind that delivery.

## Closure assessment and cleanup

One scanner state owner and one dispatcher projection remain explicit. Reader
ownership is bounded and joined, without a timeout, retry or parallel disk fanout.
No material consolidation requirement obstructs delivery or S5; no deferrals.

Retained private evidence is under `.local/s4`; baseline remains reproducible.
Native/small-file-I/O scratch: `D:/Temp/Diskweft-s4-optimization`;
disposable diagnostics: `E:/Temp/Diskweft-s4-optimization-20261104`, registered
before creation in cleanup transaction `265f13bdd6ca51c127fcefbe951b05db`.
All VHD mounts verified Attached=false after actual consumer exit. Cleanup removes
the detached outer VHDX instead of enumerating a million internal files.

Current cleanup inspected all4,383 members, signed manifest
`6e682319fc662a73f48a3d6c3ae7187abc6a293e72a3bb434460cfeb63b78bb0`.
After hash-verified evidence retention and native guest exit, its ticket applied
and removed4,383 members /2,457,915,709 logical bytes with no errors/exclusions.
Both exact scratch roots were independently confirmed absent. Previous S3/S4
cleanup is also complete. Receipts remain private; release packages are retained.
