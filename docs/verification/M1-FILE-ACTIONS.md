# Responsive file actions and clipboard correction

2026-11-04. M1 COMPLETE; M2 owns publication and actual installed update.

The user recycled a folder below AppData/Local/Tools and the UI temporarily
locked. A later Copy path succeeded, but WPF Clipboard.Flush raised
CLIPBRD_E_CANT_OPEN after the text was available to paste. Treat these as separate
defects; the clipboard error does not establish that the earlier copy failed.

## Root causes and resulting behavior

- Delete executed the shell operation inside the WPF menu handler. It now runs
  on an owned STA worker. Explicit default-No confirmation remains on the UI;
  busy guards prevent overlapping destructive actions, and normal close waits
  for the actual worker. Completion reconciles removed rows and rescans the
  affected parent subtree through the existing asynchronous scan path.
- The inherited Visual Basic delete API ignores its recycle option when the
  process is noninteractive. This was observed in the disposable guest: removal
  occurred without bin entry. Windows IFileOperation now receives explicit
  RECYCLEONDELETE/ALLOWUNDO for recycling, separate permanent-delete flags,
  early failure/error HRESULT handling and explicit COM lifetime on that STA.
  No manual permanent-delete fallback or automatic elevation was introduced.
- Copy path's WPF OLE publication/flush can fail after publication. Immediate
  CF_UNICODETEXT ownership transfer removes the later flush and persists after
  app exit. Clipboard acquisition failure leaves existing contents alone and
  reports locally. No retry loop, sleep, clipboard monitor or global setting.

Primary API references: [Visual Basic limitations](https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.deletedirectory),
[IFileOperation](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-ifileoperation),
[operation flags](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ifileoperation-setoperationflags),
[immediate clipboard ownership](https://learn.microsoft.com/en-us/windows/win32/dataxchg/using-the-clipboard).

## Inspected native evidence

Owned guest `731db7b7-8fac-47dc-8748-a5cda4c1ff98`, 4 GiB/headless/no networking or
clipboard redirection. User-installed Coordinator 0.20.4/skill 1.1.0 supplied
hash-checked sandbox.powershell with STA/process-only RemoteSigned; receipts say
host_policy_changed=false and persistent_policy_changed=false. The prior policy
rejection remains a resolved historical report.

| Acceptance | Observed result |
| --- | --- |
| Shipped recycle reproduction | Baseline source from 76dd613 with test observer only reproduces shell work on the dispatcher. |
| Clipboard reproduction | Shipped Copy path raises 0x800401D0 under a real held clipboard. This reproduces unhandled acquisition contention; the user's screenshot separately proves the post-publication Flush failure. |
| Actual recycling | Real native menu action removes the generated folder, adds one bin item, preserves its sibling and reconciles parent totals to 42 bytes / one file. |
| Permanent delete | Same real action pipeline with explicit permanent flags removes the generated target without adding a bin item. |
| Dispatcher and busy guard | An event-held shell worker permits a real dispatcher callback, selection/navigation and a refused second destructive key action. Worker is STA, dispatcherInShell=false. |
| Cancellation and close | Test-only No confirmation preserves data/idle state. Closing during a held real recycle keeps the window alive until the shell worker exits; zero owned workers at process exit. |
| Native failure | Renaming the generated target before shell lookup yields actual HRESULT 0x80070002; error reaches dispatcher, data remains in the moved folder, totals reconcile to 1,276 bytes / two files, bin unchanged, workers zero. |
| Clipboard | Actual menu Copy path under a held clipboard returns local feedback; after release, multiline Unicode round-trips and remains after the producer exits. |
| Affected UI regressions | Real classic Scan/Stop/restart/listing/selection/scroll/partial rescan/history/close, live largest-first ordering and current release notes pass. |
| Normal build | Exact baseline/fork CLI CSV parity, identity/configuration and absent diagnostic actions pass. No scanner engine changes; existing scan-performance evidence remains applicable. |
| Packages | Version/hash/config/MIT/signature and normal installed/portable CLI/native scans pass; native uninstall also returned zero and the installed EXE was absent afterward. |

Confirmation decisions and error-dialog observation are test-only seams. The
menu event handlers, real filesystem, Windows shell, clipboard and dispatcher
are exercised; test UI decisions are not claimed as physical clicks. Production
builds omit diagnostics. Raw guest state and personal paths stay in ignored
`.local/maintenance-file-actions` evidence.

Diagnostics found two harness defects: startup drive enumeration could emit idle
before the fixture scan (now waits for that fixture's terminal state), and the
PowerShell reader decoded UTF-8 Unicode text using its legacy default encoding
(now explicit UTF-8 ReadAllText). A mistaken 32-bit bin-structure packing probe
was reverted after checking the actual x64 SDK ABI; no production change depended
on it. Failed diagnostics remain distinguished from passing native acceptance.

## Qualified package identity

Normal 0.1.2, Authenticode NotSigned, MIT and DPI configuration included:

| Artifact | SHA-256 |
| --- | --- |
| EXE | `599be7ad3338d119ae89c7340cc9a876f79df2657f472416f18f08bc378ea2f5` |
| Setup | `7567776e9c33b634ed26a0c3e3e7ae50afdf6cc1c924f5dde9fb92896d28e336` |
| Portable | `03c8e4feb08978f3c4ed28d6a47165c1fb14280c86611b6fe9a18ec6b6c374cc` |

Closure assessment: existing worker ownership and async scanner are reused;
the only new shell binding serves delete, and the native clipboard path serves
plain text paths. No competing scanner, provider framework, persistent helper or
timer-driven recovery was introduced. Pathweft remains excluded and untouched.

The exact guest exited and its lease released: sandbox_exit_verified=true,
no live guest or recovery pending. Aggregate workflow exit 1 retains the earlier
negative reproduction and diagnosed harness failures; it is not presented as a
green aggregate run. Current qualifying native, persistence/regression, native
error and all four package commands each returned exit 0/isError=false, with
their actual result files inspected. Raw evidence was retained before cleanup.
