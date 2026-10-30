# S0 baseline evidence

Observed 2026-10-30. Scope: fork setup, specification and reproducible upstream
baseline. This report does not validate the planned Diskweft product features.

## Source and ownership

- Public fork: [Darkaxt/Diskweft](https://github.com/Darkaxt/Diskweft).
- GitHub bridge and `gh` independently returned fork=true, private=false and
  parent `limburatorul/spacescan`.
- Preserved source baseline:
  `313271a0bb34e63c8182c49e9c71caabbd782a4a` (SpaceScan 1.4.1).
- `origin` targets the fork; `upstream` targets SpaceScan. Default GitHub target
  is the fork. Upstream application, scanner, XAML, installer/build/check scripts,
  manifest and license remain unchanged by S0.
- Setup changes consist of project guidance, specification/plan, development
  procedure, baseline verification helpers and private-output ignore rules.

## Executed verification

Windows PowerShell 5.1 executed `scripts/Invoke-Stage0Baseline.ps1`, which called
the existing `build.cmd` and then `scripts/Test-ScannerBaseline.ps1`. The process
exited 0. The compiler uses the installed .NET Framework 64-bit compiler; no
toolchain install, framework migration or application installer was invoked.

| Observation | Actual result |
| --- | --- |
| File version | `1.4.1` |
| Assembly version | `1.4.1.0` |
| Executable SHA-256 | `1eacba02c172a76d21e7b0bed77da51783d04cfe1b0926dd2d5e51ef7e650def` |
| Authenticode | `NotSigned`; this is a local upstream baseline, not a signed Diskweft release |
| Whole controlled fixture | 7,500 bytes / 5 files, exact |
| Nested subtree | 6,000 bytes / 3 files, exact |
| Empty directory | 0 bytes / 0 files, exact |
| Largest-file statistics | 5 entries, largest 4,000 bytes, exact |
| `.bin` statistics | 7,000 bytes / 3 files, exact |
| Name exclusion | 7,000 bytes / 4 files, exact |
| Subtree exclusion | 1,500 bytes / 2 files, exact |
| Pre-cancelled traversal | 0 bytes / 0 files, exact |

The helpers load scanner types by reflection and never call GUI Main. No GUI,
installer, shell registration, updater, background service or MFT read was
started. The original self-check was inspected for reference coverage, not run
unchanged with its default Temp placement and direct cleanup.

## Evidence retention and cleanup

The ignored `.local/baseline/` holds the actual compiled executable, JSON
observations and source/compiler provenance. These assets have an
ongoing S1-S4 comparison consumer and are deliberately retained, not temporary
release deliverables. A private copy of the user's Explorer screenshot is retained
under ignored `.local/design-reference/`; its personal paths/accounts are not
published. Its SHA-256 is
`7de247aa65d059cfd05bf4922472fff4d2657b27790bf7f4ed45b6e1fc937b36`.

Process-local TEMP/TMP and all generated fixtures were confined to the exact
E: task scratch. Transaction `dce74ee795876c6572e3eb7f3c5d7f52` registered it
before generation. Review/inspection covered all 11 members, with no exclusions;
the separate ticket/apply returned state `applied`, 11 deleted members and 7,500
logical bytes. No cleanup error or residual was reported. No D: temporary
build tree or clone was created; the D: checkout is maintained source.

## Limits and next stage

No live publications, UI rendering, real network-share behavior, mid-scan
cancellation, elevated MFT, million-file performance or delivered Diskweft package
is verified by these checks. Those requirements remain assigned to S1-S4.
S0 does not implement identity isolation or publish a product release.

The next stage is S1, independent identity and the complete classic live-scanning
workflow, only when the user starts it. The current instruction ends at S0.
