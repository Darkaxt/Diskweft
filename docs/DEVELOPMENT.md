# Development and baseline verification

Read [SPECIFICATION.md](SPECIFICATION.md) and
[IMPLEMENTATION-PLAN.md](IMPLEMENTATION-PLAN.md) first.

## Source and runtime

- Canonical checkout: `D:\Projects\Diskweft`.
- `origin`: `https://github.com/Darkaxt/Diskweft.git`.
- `upstream`: `https://github.com/limburatorul/spacescan.git`.
- Default GitHub target: `Darkaxt/Diskweft`; inherited default branch is `master`.
- Upstream baseline: `313271a0bb34e63c8182c49e9c71caabbd782a4a`, version 1.4.1.
- Compile with the installed .NET Framework `Framework64\v4.0.30319\csc.exe`
  through existing `build.cmd`. No SDK/dependency upgrade is needed for S0.
- Run scanner baseline reflection checks with Windows PowerShell 5.1, matching
  the .NET Framework assembly. Loading scanner types does not invoke GUI Main.

## S0 procedure

1. Confirm upstream production/build files are unchanged and inspect `build.cmd`.
2. Create an exact task-owned scratch folder under E:; set process-local TEMP/TMP
   there for the compiler/test child. Keep the maintained checkout on D:.
3. Run `build.cmd`; inspect process result, actual assembly version and SHA-256.
4. Run `scripts/Test-ScannerBaseline.ps1` with explicit `-AssemblyPath`, fresh
   `-ScratchRoot` and `-ResultPath`. It creates a tiny controlled fixture, invokes
   existing scanner/stat/exclusion methods and checks pre-cancelled traversal.
   It does not start the GUI, use MFT, run an installer, or change registry/OS
   settings. The supplied scratch path must not exist.
5. Retain the baseline executable and report under `.local/baseline/` for S1-S4
   correctness/performance comparisons. Do not ship this SpaceScan-labelled build.
6. After the test process exits, clean the exact generated scratch with the
   installed transactional-cleanup helper. Do not call the upstream `check.ps1`
   unchanged: it uses default user Temp and its own direct deletion operations.
7. Publish a sanitized evidence summary under `docs/verification/`, reconcile S0,
   commit/push setup and independently verify the fork's delivered source head.

Example test invocation (substitute the task's exact paths):

```powershell
powershell.exe -NoProfile -File scripts/Test-ScannerBaseline.ps1 -AssemblyPath .local/baseline/SpaceScan-1.4.1.exe -ScratchRoot E:/Temp/Diskweft-stage0/scanner-fixture -ResultPath .local/baseline/scanner-result.json
```

For a fresh baseline, `scripts/Invoke-Stage0Baseline.ps1 -ScratchRoot <fresh-path>`
performs steps 1-5 using the existing build script and test helper. Run it with
Windows PowerShell 5.1. It refuses an existing evidence directory or build output
so it cannot silently overwrite a retained baseline. Inspect or deliberately
select a separately reviewed baseline workspace for later reproduction.

The fixture checks ordinary final scanner behavior only. It is not proof of
concurrent publication, Windows theming, MFT correctness, performance or packaged
Diskweft behavior. Upstream `check.ps1` supplies reference coverage for additional
search/duplicates/history/integration checks in later stages.

## Private and generated material

`.local/` is ignored. It contains a private copy of the user's Explorer screenshot
and retained baseline assets. Completed disposable policy-preflight inputs/reports
are cleaned through the installed helper. Baseline binary/hash
retention has an active S1-S4 comparison purpose; temporary fixtures do not.
No new D: temporary exception is required for S0's small E: fixture.

Do not launch the inherited application against the user's profile: it still
publishes SpaceScan's ecosystem card, registry identity and update endpoints.
Do not run the inherited installer, which shares SpaceScan's AppId and old-data
migration. S1 must replace these identities before ordinary runtime trials.

Real MFT reads and administrator installers use the installed administrator
broker with genuine per-command consent. No administrator action is needed for
S0. Discover the installed Inno Setup compiler and adapt package identity in the
delivery stage; S0 does not require installing packaging tools.

## Design application

The private Explorer reference and Microsoft Windows conventions take priority.
Huashu supplied design-context, hierarchy/readability and local-runtime guidance.
This request uses that skill as inspiration for a native WPF product, not a request
for three HTML prototypes or a design-selection checkpoint. Production design
verification belongs to S3 and uses actual WPF rendering.
