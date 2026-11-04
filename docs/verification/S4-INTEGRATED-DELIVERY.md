# S4 integrated verification and delivery

2026-11-04. S4 is BLOCKED on externally unavailable administrator execution.
S0-S3 are COMPLETE; S5 is NOT STARTED.
The product outcome is incomplete: no release has been published and installed
SpaceScan has not been replaced.

## Verified current scope

- Classic performance: actual 1,000,000-file NTFS fixture on D:, 1,011 directories,
  747,520,000 logical bytes; 90% empty files, 9% 1 KiB, 1% 64 KiB, equal .bin/.txt
  names. Three paired runs after one warm-up per engine, alternating first engine,
  no cache flush and the same concurrent sequential cleanup inventory throughout
  the final paired set. Each measurement uses a fresh process (cold JIT, warm file
  cache); the unmeasured warm-ups warm only the filesystem cache. Windows 11 Pro build 26200,
  i9-13900HX (24 cores / 32 threads), approximately 64 GiB host RAM.
- Baseline is unchanged SpaceScan 1.4.1, source `313271a`, retained binary SHA-256
  `1eacba02c172a76d21e7b0bed77da51783d04cfe1b0926dd2d5e51ef7e650def`.
  Scanner methods are invoked without starting its inherited GUI or installer.
- Exact classic totals match on every run. Paired wall times, milliseconds:

  | Pair | Baseline | Diskweft | Baseline CPU | Diskweft CPU |
  | --- | ---: | ---: | ---: | ---: |
  | 1 | 462.7407 | 521.5165 | 468.750 | 515.625 |
  | 2 | 500.0875 | 531.8430 | 500.000 | 531.250 |
  | 3 | 521.0105 | 515.1429 | 546.875 | 500.000 |

  Medians: 500.0875 / 521.5165 ms, **+4.2851%**. The median of paired increases
  is **+6.3500%**; both pass the <=10% gate. Individual increases were +12.7017%,
  +6.3500% and -1.1262%. Process peak working sets: baseline
  30,646,272–30,789,632 bytes; fork 27,963,392–28,131,328 bytes.
  Publication copying: 6.8731–7.1604 ms/run.
  CPU numbers are process CPU deltas including invocation/setup; wall timing
  brackets the scanner call. Raw evidence's early `peakPrivateBytes` field is
  actually .NET PeakPagedMemorySize64, not private working set; the harness now
  names that field `peakPagedBytes`. Peak working-set values are correctly named.
- An unconsumed mailbox retained at most 1,011 directory values and 1,000 largest
  files across 7–8 revisions. It coalesced by directory rather than retaining a
  per-file event history.
- Native million-file workflow in the owned 4 GiB headless guest, reading the
  same real fixture through its readonly host mapping: scan start to first useful
  resolved producer publication 292.2872 ms; publication to useful WPF folders
  337.6937 ms; scan start to useful WPF folders **629.9809 ms**. The metadata
  timestamp is the resolved producer boundary, not the first raw OS record.
  P95 dispatcher action latency 3.0725 ms, maximum 294.7738 ms over 85 samples.
  The earlier full native/package workflow also passed (p95 13.7444 ms).
  Navigation during scan and exact terminal
  totals passed; all owned workers exited. Diagnostic cadence 50 ms, with at most
  one outstanding action. This measures the WPF dispatcher boundary, not physical
  input-to-photon latency or raw-volume throughput inside the guest.
- The complete affected native classic regression passed after optimization:
  virtualized selection/scroll, keyboard expansion, stop/restart, stale listing
  rejection, subtree rescan/history, busy delete refusal and thread-aware close.
  Controlled barrier evidence is a responsiveness check, not throughput.
  The final current-source run measured 512.1879 ms at that boundary.
- Current-source classic scanner regression passed partial ancestors, exclusions,
  cancellation/draining, failure provenance, long paths, actual restored ACL
  denial, real read-only UNC, denied raw-read fallback, search, duplicate hashing,
  history/compare and updater identity. Current reordered-MFT regressions passed.
  Unchanged S2 real-volume correctness evidence remains applicable; it does not
  substitute for S4's paired performance gate.
- Normal 0.1.0 candidate passed baseline CLI CSV parity, HTML, independent profile,
  registry/card/updater identities and absent production test hooks. Installer and
  extracted portable package both ran their actual normal CLI and native WPF
  scan to exact totals in the disposable guest. Installed shell command and
  uninstall passed, preserving SpaceScan sentinels. Actual default installation
  is Program Files (x86)/Darkaxt/Diskweft; AnyCPU execution is unchanged.

Final native/package workflow: Coordinator ticket
`bf80b4a020ff42b1bd8db5508984de5d`, exact guest
`bf80b4a0-20ff-42b1-bd8d-b5508984de5d`, request
`s4-final-current-native-and-packages`, native exit 0/isError=false. The aggregate
workflow also exited 0; actual guest exit and lease release were verified.
This current-code run supersedes earlier native/package diagnostic attempts.
The final fallback-cache correction was then red/green tested against an actual
classic source followed by denied raw-MFT access and cancelled replacement.
Rebuilding global statistics now synchronizes the last classic extension snapshot,
so its unflushed tail cannot replay. The ordinary million-file code path is
unchanged by this fallback-only correction; its inspected performance/latency
evidence remains applicable. Final normal-package installation, CLI, WPF and
uninstall were independently rechecked under ticket
`11f2425006a54ab79ac76f92f79df5e4`, request
`s4-final-mixed-fallback-package-verification`, native exit 0/isError=false.
The explicit start/publication/WPF timing run used ticket
`449299d2755e42e5a022bbab30d317ac`, native request
`s4-explicit-latency-boundaries`, exit 0/isError=false. Its timing-only diagnostic
change leaves the scanner and packaged binaries unchanged.

The initial classic result missed the gate (+36.1%). The first optimization's
ratio of medians passed (+8.1%), but its median paired increase still missed
(+12.8%); it was not accepted. The final implementation reuses the parsed extension,
batches producer updates in groups of at most 64 files and flushes extension
deltas at publication/directory transitions. Default enumeration reads the static
cancellation flag directly; explicitly supplied listing cancellation remains
request-scoped. Removing the per-file cancellation delegate closed the remaining
hot-path gap. The final complete paired comparison passed both calculations;
prior unsuccessful diagnostic runs remain private evidence.

Upstream drift was inspected through `a33675a` (1.4.4). No scanner change required
adoption. The relevant updater original-user launch correction was incorporated;
Setup retains runasoriginaluser and can suppress relaunch with /NOLAUNCH during
managed installation. Unrelated donation UI was outside the requested product.
Interactive update token transitions have not been claimed as guest-tested:
the headless guest executes as SYSTEM. The final host launch belongs to S5.

## Candidate packages, unpublished

Version 0.1.0, Inno Setup 6.7.4, MIT included, Authenticode NotSigned.

| Item | SHA-256 |
| --- | --- |
| Packaged Diskweft.exe | `dead336c9a3b1e5e53bd88f641471fb58c899f088162eebf92def9a376d120da` |
| Diskweft-0.1.0-setup.exe | `fa1b4097585fdc4841980e500fc37ac8f7d876c7d8d8e5294d5230298f2e799e` |
| Diskweft-0.1.0-portable.zip | `7b113e2308594c24e9bb6d8232023ceafcc694e42062356eff3e11bdffa60fe9` |
| Runtime DPI config | `28472e5da425eccd7d67a8d830d94524e8bf08ef321991731dbdda2d07a1bc75` |

These are retained local candidates under `dist/0.1.0-final-candidate`, not a delivered release.
Publishing, independent retrieval, final source/tag correspondence and installed
system replacement remain unfulfilled. Do not run the host installer in S4.

## Remaining requirements and exact next action

PERF-01: three paired raw-NTFS baseline/fork runs are blocked externally by the
administrator broker's fullscreen quiet mode. Request
`4924a5a003cf4ba89ac410cbaa7e3134` returned `fullscreen_active`, launched=false;
no command executed. The user was asked to return and confirm readiness for a
fresh broker request. Do not automatically retry or change execution routes.

Once that direction arrives, submit one broker request for
`scripts/Invoke-PerformanceChecks.ps1 -Mode Ntfs`, binding the current retained
benchmark in `.local/s4/benchmark` (pass its path as -BuildRoot) and baseline
binaries. Inspect original completion, close any measured
performance defect and reverify affected workflows before DELIVER-01 publication.
DELIVER-01 depends on that gate; S5 depends on independently verified delivery.
No current requirement has been waived or deferred to an unnamed future task.

## Retention and cleanup

Current candidates, normal development executable, hash-pinned pending NTFS
benchmark and required results remain under `dist/0.1.0-final-candidate` and
ignored `.local/`. Temporary native/test inputs are gone; all owned guests exited.

Transaction `ce60ad68c2573d5372bb70cc9c4a4212`, signed manifest
`e16dbe655f6f3536973e5254e64f2153f23dd3794af713c82c3792fd85a288fa`, inspected all
1,005,525 members. Its apply removed 1,005,523 members / 760,498,758 logical bytes;
two parent directories remained nonempty with diagnostic files created after the
review. Those exact residuals received a fresh review under transaction
`8c3a90d6dab8031bb5ec06417ed66506`, signed manifest
`c3c32e366ba1352b53ffee5c48809a0fc27a1733d68e80b35b6b749305e68ed5`:
4,295 members / 208,067 bytes removed. Resuming the original ticket then closed
it as applied, with both parents already absent. No errors/exclusions/residuals.

Superseded candidate cleanup `717f5689e74c0c45f782cbf39e320100` removed four members
/ 2,734,718 bytes. Final package-guest/superseded candidate cleanup
`b7e78a773ea27fe46f3476fd045ca57b` removed 57 members / 3,262,222 bytes. These
transactions also closed as applied. Counts include directories; byte totals are
logical helper-reported values, not claimed physical space recovery. Exact
`D:/Temp/Diskweft-s4-fixture`, `D:/Temp/Diskweft-s4-build`,
`D:/Temp/Diskweft-s4-package-final` and `E:/Temp/Diskweft-s4-20261104` were confirmed
absent. No cleanup, compilation, guest or installer operation remains running.
