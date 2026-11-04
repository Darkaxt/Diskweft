# Issue report: disposable guest rejects the verification entry point

Observed 2026-11-04 while investigating Diskweft's recycle UI stall and subsequent
Copy path Clipboard.Flush exception. This report concerns the test environment;
it does not establish a passing or failing application regression result.

Resolution update, 2026-11-04: the user installed Coordinator 0.20.4 and directed
resumption with the updated skill's `sandbox.powershell` method. The actual
hash-checked route executed reviewed verifiers with STA/process-only RemoteSigned;
receipts confirm host_policy_changed=false and persistent_policy_changed=false.
The original denial remains historical evidence, not a current blocker. Native
application workflows now run; see the maintenance verification record.

## Expected result

The Coordinator-owned disposable Windows guest runs the reviewed file-action
verification script, which starts the generated test executable against guest-only
folders and clipboard. The script records red/green results in the mapped output.

## Actual result and evidence

The guest started successfully. Both attempts to enter the script returned native
exit code 1 before script entry. The second invocation captured stderr, after the
first invocation produced no script-entry output. Neither application test ran.

Exact Windows diagnostic:

```text
File C:\Coordinator\Work\Verify-FileActionsGuest.ps1 cannot be loaded because
running scripts is disabled on this system. For more information, see
about_Execution_Policies at https:/go.microsoft.com/fwlink/?LinkID=135170.
    + CategoryInfo          : SecurityError: (:) [], ParentContainsErrorRecord
   Exception
    + FullyQualifiedErrorId : UnauthorizedAccess
```

Guest command:

```text
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -STA -File C:\Coordinator\Work\Verify-FileActionsGuest.ps1 -Mode Red
```

The diagnostic wrapper only redirected that same entry point's output to a file.
No execution-policy override, signing/trust change or alternate test execution
route was used after the rejection.

## Environment and exact ownership

- Installed Coordinator release: `9d66134582d2ae2b`; launcher resolves and verifies
  the activated installed runtime rather than using the development checkout.
- Installed Sandbox skill 1.0.0, pinned WinApp 0.7.1; 4,096 MiB, headless,
  networking and clipboard redirection disabled; execution context `System`.
- Original workflow ticket: `d9f871c8eb324d5ba016485b8218cc69`.
- Original guest: `d9f871c8-eb32-4d5b-a016-485b8218cc69`.
- Host mapping: `D:/Temp/Diskweft-file-actions-20261104/sandbox` to
  `C:\Coordinator\Work`.
- Failed request IDs: `red-file-actions`, `inspect-entry`; both reported
  `isError=true`, native `ExitCode=1`, `host_fallback=false`, `run_as=System`.
- Submitted script SHA-256:
  `cc525651154f1d86cb38d1f13c8f11ace9d25da8f224b649c53340a76b41c66b`.
  The maintained source script was subsequently extended with additional cases;
  this hash identifies the version actually submitted to the guest.

The first client was launched without an interactive stdin. Its guest remained
owned and was recovered by attaching an interactive client to the same live
workflow ticket. No duplicate guest was launched. That channel issue was resolved
before the two script requests and is separate from the policy rejection.

## Diagnosis and resolution boundary

Windows' effective guest script policy rejects this unsigned `.ps1` entry point.
The exact effective policy value and whether a particular policy scope supplied
it were not queried; do not treat `Restricted` or a managed-policy cause as proven.
This is an execution-policy diagnostic, not an application failure, admin-broker
rejection, unavailable Sandbox feature or failed guest startup.

The owner elected to handle the issue after being offered a process-only guest
override. No override was authorized or applied. Resolution requires an owner-
approved way to execute the reviewed verifier in this disposable guest; do not
alter host policy or silently substitute another execution route.

## Impact and resumption

FIX-01/FIX-02 acceptance is blocked by missing native regression evidence.
Source fixes and tests are present locally; a diagnostic build compiled, and
`git diff --check` passed. Neither result proves the fixes work. Installed and
published Diskweft remains v0.1.1; no update, publication or unverified fix commit
was performed. Live user folders, clipboard and application were left untouched.

Once the owner resolves the guest execution policy, build the current tests and
run the file-action regressions plus applicable classic/live-order/package checks
through the installed Sandbox gate. Complete M1 before release/install stage M2.
Preserve both reported defects: synchronous shell recycle and the post-copy WPF
flush exception even though copied text was successfully pasted.

The owned guest ended; Coordinator readback confirms `sandbox_exit_verified=true`,
workflow complete, no live lease and no recovery pending. Exit 1 records the
rejected commands, not passing tests. Both original/resumed client sessions exited.
Cleanup transaction `063706a0704071711937709fa0e56480` applied its fully inspected
manifest: all 14 members / 1,313,325 logical bytes, no errors or exclusions.
Disposable build/guest scratch is removed; rejection text and ownership receipts
remain in private maintenance evidence. Rebuild from maintained sources on resume.
