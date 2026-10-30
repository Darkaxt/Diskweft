# Diskweft specification

Version 0.3 - 2026-10-30. Owner: Darkaxt.

This is the authoritative specification. The implementation plan cannot weaken
its behavior, constraints or acceptance criteria. The user's standing engineering
instructions govern execution, verification, cleanup and release authority.

## 1. Outcome and authorization

Create Diskweft, a distinct SpaceScan fork that shows useful folder results while
scanning, including the NTFS fast path. Keep scanning fast and navigation stable.
Use Windows 11 appearance and the user's Explorer screenshot as the primary
visual reference, including Windows accent colors. Use Huashu as inspiration for
scan-specific elements where Windows conventions do not supply an answer.

The initial 2026-10-30 request established the specification and S0. S0 is
COMPLETE. The subsequent user instruction, "Start stage 1", authorizes S1's
independent identity and complete classic live-scanning workflow, including its
integration, verification, commit and source synchronization. **The current
execution boundary is S1 only.** S2-S5 remain NOT STARTED; no intermediate product
release is authorized. The standing final-release rule applies in S4 after the
whole product scope and its prerequisite verification pass.

The user subsequently requested a Stage 5 to replace SpaceScan in the system
with this fork. This adds the post-release migration requirement MIGRATE-01 and
authorizes documenting its stage now. It does not start the migration during S1.
S4 remains the final product release; S5 owns the separately requested system
replacement after that release has been delivered and verified.

No new framework migration, filesystem indexer, background service, scheduled
monitor, broad parallel scanner, treemap or unrelated file-manager work is part
of this specification. S1 uses Pathweft as reference context and does not change
its repository or installed settings. S5 may update an actual SpaceScan caller
route in Pathweft when the migration inventory establishes that consumer; it
does not authorize unrelated Pathweft development.

## 2. Sources and design authority

| Source | Pin / role |
| --- | --- |
| SpaceScan | `limburatorul/spacescan`, `313271a0bb34e63c8182c49e9c71caabbd782a4a`, version 1.4.1; WPF/.NET Framework, MIT |
| Fork | Public `Darkaxt/Diskweft`, preserving SpaceScan ancestry |
| Pathweft | `Darkaxt/Pathweft`; remote main observed at `b8c75278c74ce5a7728affa60ef9ef327dbcae3d`; naming/design context only |
| User Explorer screenshot | Private retained visual reference; contains personal paths/account names and must not be committed |
| Windows design | [Color](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/color), [DWM attributes](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute), [system backdrop](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type) |
| Huashu | Installed 3.0.0 guidance, read with local runtime and design-context references; subordinate to Windows/user requirements |

Recheck relevant upstream drift before adoption or final packaging. Do not blindly
replace fork changes. Keep attribution and pin any imported dependency. The
Pathweft checkout contained active changes during S0; none are imported or treated
as verified Diskweft behavior.

## 3. S0 setup requirements

| ID | Required outcome and acceptance |
| --- | --- |
| ENV-01 | Named public fork preserves history. GitHub reports `Darkaxt/Diskweft`, public, fork=true, parent `limburatorul/spacescan`. |
| ENV-02 | Maintained D: checkout, origin/upstream and default GitHub target correctly owned. Commit/push this setup scope, verify remote head and clean tracked worktree. |
| ENV-03 | Establish a reproducible upstream baseline without installing/launching the GUI. Build with existing `build.cmd`, inspect version/hash, and run controlled scanner checks for totals, exclusions, top-file/extension statistics and pre-cancelled traversal without requesting elevation. Preserve source identity and record coverage limits. |
| ENV-04 | Commit the specification, compact stage map, design contract, development procedure and actual S0 evidence. Map every product requirement to a stage and verification method. Keep personal references/private diagnostics out of Git. Clean expendable generated scratch through the installed helper. |

## 4. Product requirements

| ID | Required behavior and acceptance |
| --- | --- |
| ID-01 | Independent Diskweft executable/title/metadata/icon, settings/history/logs, registry, ecosystem card, shell verbs, installer/uninstaller and updater/release endpoints. Side-by-side use must not overwrite, redirect, migrate or uninstall SpaceScan or Pathweft. Prove isolation before ordinary application trials. |
| SCAN-01 | Ordinary scans publish discovered directories, growing byte totals and recursive file counts before completion. A large unfinished child must contribute to its ancestors. Empty, denied, excluded and unvisited directories must not be presented as completed zero-size results. Preserve long-path/UNC handling and reparse-point policy. |
| SCAN-02 | Whole-volume NTFS/MFT scans also publish useful folder results during record reading. Resolve parents arriving out of order; apply ancestor/path exclusions without retaining excluded totals or double counting. Show pending/unresolved metadata honestly. Phase text alone does not satisfy this requirement. |
| SCAN-03 | The scanner owns working state; the WPF dispatcher owns displayed collections. Publish consistent, coalesced batches with stable per-scan directory identity. No queue item per file, unbounded pending event history, whole-tree clone every tick, or scan-thread wait for rendering. Progress-only retained state scales with directories/visible data, not all files. |
| SCAN-04 | Completed, cancelled and failed are distinct terminal states. Drain/reconcile final updates exactly once. Stop/restart, MFT failure/fallback, all-drives scans and partial rescans cannot accept stale updates, reuse a cancelled generation or double-count totals. Never mark a cancelled/failed attempt completed; a successful replacement scan may complete with its actual engine and fallback reason shown. |
| SCAN-05 | Expand/collapse, selection, keyboard navigation and scroll remain usable during scanning. Preserve row identity and expanded state across updates; no forced scrolling or constant resorting. Disk enumeration/file listing runs off the UI thread. Clearly label files not yet listed; do not allocate every scanned file merely for navigation. |
| SCAN-06 | Publish extension/category totals and bounded largest-file results through the same consistent publication boundary. Existing statistics caps remain bounded. Partial file paths waiting for MFT ancestors are visibly pending or withheld until valid, never invented. Percent bars mean share of discovered bytes, not completion percentage. |
| SCAN-07 | Partial states are visible and accessible: pending/scanning, completed, stopped and inaccessible distinguishable by text/state as well as color. Classic traversal has no fabricated completion percentage; MFT record progress is labelled as that phase rather than overall work. Do not promise monotonic provisional totals when late exclusions require correction. |
| UI-01 | Windows 11 is the primary appearance target: neutral surfaces, Segoe UI family, compact command bar, normal Windows window/keyboard behavior, restrained separators/selection/focus and readable tabular data. Preserve useful scan views; no marketing layout, oversized hero metrics or decorative cards around each folder. |
| UI-02 | Default to the Windows app light/dark preference and system accent. Provide System/Light/Dark mode plus system/custom accent selection in Appearance. Persist app overrides and apply them consistently to the shell, scan views, menus, dialogs and settings. Observe Windows changes while open without changing OS settings or restarting a scan. |
| UI-03 | Honor high contrast, readable focus and contrast independently of accent choice. Use system/window colors in high contrast. Native DWM materials/corners/title-bar behavior are capability-checked; unavailable composition uses coherent solid neutral surfaces. No dependency/framework migration solely to obtain a material effect. |
| UI-04 | Verify actual WPF rendering at the design-contract sizes/scales, including live scanning, expanded trees, empty/denied/stopped states and Appearance. Record structured UI state and targeted screenshots. HTML inspiration is not native acceptance evidence. |
| COMPAT-01 | Preserve existing folder/drive/all-drives scans, exclusions, search, duplicate detection, history/compare, HTML/CSV/CLI reports and safe file operations. Changes must not enable destructive actions against actively mutating scan results. Diskweft snapshots preserve terminal/partial provenance so cancelled scans are not silently used as complete history baselines. |
| PERF-01 | On a declared representative classic million-file fixture and a large NTFS volume, measure at least three paired baseline/fork runs under comparable cache/load conditions. Median scan-time increase must be <=10%; report peak memory/CPU and publication overhead. No backlog grows with total file count. Missed limits are blockers, not waived by small-fixture success. |
| VERIFY-01 | Verify real local and accessible slow/network paths, real elevated NTFS access, fallback, controlled final totals, UI responsiveness and restart/cancellation. Compare unchanged controlled fixtures exactly against the retained baseline; do not require two live system-drive scans to match exactly. Mock/reordered-record tests supplement real boundaries. |
| DELIVER-01 | After S1-S3 and pre-release checks pass, publish one normal Diskweft v0.1.0 release on this fork with a usable installer, portable package, MIT notices, checksums and accurate notes/source commit. Independently retrieve the delivered assets and verify version, hashes, signature status, packaged identity/configuration and representative installed/portable behavior. No intermediate release or upstream SpaceScan installer substitution. |
| MIGRATE-01 | In S5, inventory the actual installed SpaceScan and its settings/history, shortcuts, registry/shell verbs, ecosystem cards and calling applications (including Pathweft if applicable). Install the verified Diskweft release, copy/convert relevant user data with verified preservation and explicit partial-snapshot provenance, and replace the discovered SpaceScan launch/integration routes. Verify real launches, data/history access, caller routing and rollback before retiring obsolete registrations/install files. Preserve source, original data and a concrete recovery path; do not blindly redirect unrelated applications or delete ambiguous files. Ordinary administrator actions use the supported broker. |

## 5. Publication and lifecycle design

Keep the current WPF/.NET Framework architecture and both scanning engines.
Introduce only the scan-owned publication state and UI projection required by the
live feature. Coalesce at the producer/publication boundary, then apply changes
to existing rows on the dispatcher. A nominal 500 ms display cadence is an
explicit presentation requirement, not a completion/cancellation timeout.
Terminal publication is event-driven and does not wait for the next timer tick.

For classic traversal, accumulate direct-directory observations and propagate
coalesced aggregate changes without repeating completed-child contributions.
Where practical, discover immediate children before descending so one large
subtree does not hide all siblings. Do not add broad parallel disk I/O for this.

For MFT, keep the existing raw-reader/fixup/runlist logic and compact record
storage. Resolve directory relationships progressively with pending ancestry;
do not rerun `BuildTree()` and `Sum()` over all records every refresh. Only claim
directory completion when the actual processing state justifies it. Define how
late ancestor exclusions retract provisional contributions and how fallback
replaces the abandoned MFT projection before publishing classic totals.

Per-scan identity/generation includes its source phase/attempt. A terminal result
owns its final projection and history handoff. A previous history-save callback,
cancelled file listing or obsolete MFT batch cannot alter the next scan.

## 6. Observable acceptance and design contract

- On a responsive fixture, publish the first useful folder results within 1 s of
  usable metadata becoming available. Report start-to-metadata separately; slow
  or blocked filesystem calls must not be hidden behind a false start guarantee.
- During a still-running scan, publish changing parent/child sizes and permit
  real expand/select/scroll actions. Target <=100 ms p95 dispatcher action latency
  on the declared million-file test host; report the measured boundary and load.
- Refresh nominally every 500 ms while changes exist, coalescing rather than
  queueing obsolete frames. Final reconciliation is immediate on terminal delivery.
- Use WPF DIPs: minimum usable client content 960x560, primary 1280x820 and wide
  1600x1000; verify 100%, 150% and 200% display scale where the physical viewport
  permits those client bounds. At smaller physical bounds use an accessible
  scrolling/column-reduction layout without hiding scan/stop/navigation controls.
- Default text is Segoe UI/Segoe UI Variable, 14 DIP body and >=12 DIP secondary;
  rows 28-32 DIP, command controls 32-36 DIP, 4/8/12/16 DIP spacing and 4 DIP
  control corners. Native caption buttons retain Windows hit targets. Dense
  desktop controls follow Windows conventions rather than web touch-target rules.
- In dark mode, begin from the supplied reference's near-black content and
  slightly lighter command/header surfaces; light mode uses corresponding
  neutral whites/grays. These are design directions, not sampled exact pixels.
- Accent identifies primary actions, focus, selection and data bars sparingly.
  Regular text remains neutral. Text contrast >=4.5:1; interactive/non-text
  indicators >=3:1 where applicable. Preserve contrast for very light/dark custom
  accents and never encode completeness solely by color.
- Keep scan status/stop discoverable, columns aligned and sizes readable. Stable
  sibling order during active interaction; explicit user sort or deliberate
  completion sorting must preserve selection/scroll context.
- Verify light/dark, multiple accents, high contrast, live setting changes and
  restart persistence on the actual native app. Do not change host appearance
  settings solely for testing when an isolated test override can exercise the
  same resolution path; separately qualify actual Windows notification handling.

Primary tested OS is Windows 11. Preserve existing .NET Framework portability
and capability-check new Windows APIs; do not advertise untested older Windows
versions as verified. Native materials are an enhancement; readable theming is
required regardless of compositor availability.

## 7. Verification, delivery and scope boundaries

Use isolated application profiles and generated fixtures. The S0 baseline is
compiled and inspected, not installed or launched. Real MFT acceptance belongs
to S2/S4 through the administrator broker with its genuine per-command consent;
gate availability never proves that the underlying MFT call succeeded.

Use the existing self-check as reference coverage, extending/reusing relevant
tests without its uncontrolled temp placement or bespoke cleanup. Implement
feature-relevant native diagnostics alongside the complete feature: stable
actions, scan revision/phase/counters, instantiated row state, readiness events
and monotonic timings. No permanent debug server or production telemetry service.

S4 integrates all required behavior and release readiness, then commits the
verified result and publishes/verifies the normal release. Installer signing
must report actual status; do not fabricate a signing identity or change trust
configuration. Installation tests preserve side-by-side identities and are
isolated or explicitly authorized; publishing does not imply production rollout.

S5 is the expressly requested rollout boundary. Its inventory must determine the
actual installation and consumers at execution time. Do not rely on current
source identities as proof of installed state. Retain rollback information and
verify the final installed package and caller routes. S4 product delivery can
close before S5, but the expanded system-replacement outcome requires S5 as well.

Private reference images and machine diagnostics stay in ignored project-local
storage; publish sanitized evidence summaries. Retain the baseline executable
and its hashes for S1-S4 comparisons. Clean generated fixtures/download/build
scratch when their actual consumers finish using transactional cleanup.
