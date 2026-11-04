# S2: progressive NTFS/MFT scanning

Verified 2026-11-04. Scope: progressive MFT scanning and its integration with
classic scans. Current internal build: `0.0.2-stage2-development`.

## Implemented outcome

The raw MFT reader now publishes resolved folders, ancestor totals, extension
statistics and bounded largest-file candidates during record reading. A directory
is attached only when its full ancestry is known. Files with unresolved ancestry
or path exclusions remain withheld; incomplete resolution remains visibly partial
at the end. The existing fixup, runlist and fragmented-MFT reader remain in use.

MFT working state is owned by the scanner. `MftProgress` coalesces directory
changes into the existing `ScanSession` mailbox; the dispatcher updates its own
stable rows. There is no per-file event queue or repeated whole-tree rebuild.
Path-exclusion candidates retain compact record IDs until ancestry resolves,
then reread their metadata and release that pending group. Largest-file names
are selected from compact metadata and only the bounded winners reread at the
end, so excluded or unresolved large files cannot crowd out valid final entries.
Extent corrections use both record and directory identity across volumes.

A failed MFT attempt explicitly removes its projected descendants, resets its
scope and statistics, cancels stale listings and advances the source attempt
before publishing classic replacement results. Stop and native close continue
to wait for actual owned worker completion. Mixed roots accumulate each source
once into the synthetic This PC parent.

## Acceptance evidence

| Criterion | Inspected evidence |
| --- | --- |
| SCAN-02 | Reordered raw FILE records passed through the actual parser/publication path; an early file became visible when its late parents resolved, before EOF. A real elevated E: NTFS scan published live folders and statistics. |
| SCAN-03 | Directory-keyed coalesced batches, scanner/dispatcher ownership and bounded largest-file projection. Compact reader metadata is retained; unresolved paths are not invented. |
| SCAN-04 | Real NTFS stop/restart, native failed-attempt replacement after a controlled exception following actual raw-volume reads, and terminal stopped provenance passed. Actual unprivileged raw-access denial followed by classic fallback also passed in regression checks. |
| SCAN-05 | Actual native tree expansion, asynchronous listing, directory selection and selection preservation through real NTFS terminal reconciliation passed. Classic virtualized-scroll regression remains passing. |
| SCAN-06,07 | Raw-record fixtures proved late ancestor/file exclusions, exact direct/ancestor/extension accounting, extent correction and withholding of orphan data. A separate-volume largest-file seed with an identical record number remained present during correction. Real unresolved records produced INCOMPLETE rather than complete history. |
| affected COMPAT-01 / VERIFY-01 | Classic/UNC/exclusions/long paths/actual ACL denial, search, duplicate hashes, snapshots/history/compare, restart/stale-listing refusal, normal CLI baseline CSV parity and identity isolation passed. Real mixed MFT/classic roots reconciled exact parent/child totals. |

The complete elevated native workflow returned `ok=true`, `launched=true`,
`elevated=true`, `command_exit_code=0`, no stderr/truncation, broker request
`b87b7479cc4c4e82861380dfc7bc9f23`. The native application waited for all owned
workers before exit. It read the actual E: volume, then exercised failure and
cancellation and a mixed scan with a 731-byte classic fixture on D:.

Useful real MFT results reached the WPF dispatcher in **462.1002 ms** from the
declared metadata-publication barrier. The inspected native capture was busy,
revision 2, with 821 known directories and five mounted rows. This is offscreen
native WPF evidence, not physical pixel presentation or a throughput benchmark.
The native run's assembly metadata was 0.0.1; afterward the development label was
updated and a same-record-across-volumes correction was regression-tested through
the actual raw-record parser. The unchanged raw-volume/native paths retain that
real-boundary evidence; S4 independently verifies the final packaged build.

Prior native harness failures were not counted as acceptance. Traces established
a queued listing assertion running in a subsequent generation, a repeated
fallback-terminal assertion, and overlapping synthetic all-drives fixture roots.
Assertions are now scoped to their original transition and mixed roots are on
separate volumes, matching actual all-drives inputs. The initial UNC fixture had
also been retired; the accessible replacement was scanned read-only. No remote
files were created or modified.

The final current-source normal build passed the independent CLI/identity check,
including absence of MFT record observers/replay hooks. Its SHA-256 is
`81c57d6c4022a5d77a9962b7b142424208f1a53065904467f143891e36171fcf`;
the retained development copy independently matches. Authenticode: `NotSigned`.
No certificate/trust configuration, installation or system caller route changed.

## Retention and remaining scope

Ignored `.local/s2/` retains current source hashes, final raw-record and normal
results, applicable classic regression results and selected actual native
captures for S3/S4 regression use. These private assets are intentionally retained;
machine paths and private endpoints are not published. `.local/development/`
contains the current development executable and MIT notice.

After owned consumers exited, transactional cleanup
`ce2911353ee5f3c99e6636476f3071b2` applied the separately ticketed signed manifest
`84e264acacf9a0c8993bdf589622e9a51f39d8f8b605cbd0132752280c8fd36a`:
8,728 members / 6,729,442 logical bytes removed, no exclusions/errors/residuals.
The exact E: test and D: native build roots were confirmed absent. D: was used
for compilation and the separate-volume mixed-root fixture; no active build or
test process remained. Retained project evidence was outside those roots.

S2 has no required tracked deferrals. Windows appearance/accent work belongs to
S3. The paired million-file/large-volume performance and final installer/package
acceptance remain S4. Installed system replacement remains S5. This stage does
not publish an intermediate product release.
