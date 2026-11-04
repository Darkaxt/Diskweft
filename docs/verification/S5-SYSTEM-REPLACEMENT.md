# S5 installed-system replacement

2026-11-04. **COMPLETE.** MIGRATE-01 passed; no remaining acceptance criteria,
blockers or tracked deferrals. This is the separately authorized system rollout
of the published S4 release, not another product release.

## Acceptance reconciliation

| Required outcome | Inspected evidence and result |
| --- | --- |
| Actual installation and consumer inventory | SpaceScan 1.4.1, its installed executable/uninstaller, profiles/history, five registry exports, two Start menu shortcuts and ecosystem card inventoried before mutation. Pathweft was inspected read-only and explicitly excluded by the user. |
| Independently delivered package installed | Normal v0.1.1 from source/tag `76dd613b514ef9ab32744609de32972407581560`; installed EXE SHA-256 `9903e7e15ec2f8a2931582187f69bf2903b7c891327057e6a2e0b60b6f6f1046` matches independently retrieved portable/guest-tested setup output. Installed version and DPI configuration match. Authenticode remains NotSigned. |
| Installed launch routes | Actual `%ProgramFiles(x86)%\Darkaxt\Diskweft\Diskweft.exe`, Start menu shortcut, independent executable registry key and `diskweft` ecosystem card point to version 0.1.1. Folder/drive verbs quote `%1`; directory background quotes `%V`. |
| Actual application and profile | Normal installed EXE process received a bare folder path, exposed its real WPF status through read-only UI Automation, completed the one-file/1,234-byte classic scan and closed normally with exit 0. Readiness/completion followed native events. Installed assembly native workflows also passed using the real profile; no test-profile override. |
| Real installed raw NTFS | Broker-owned installed native workflow used the real E: raw MFT path: 1,189,208 files / 872,019,021,454 bytes, zero owned workers at terminal idle. Unresolved live records were honestly withheld and labelled incomplete, rather than falsely called complete. No classic fallback was accepted as raw evidence. |
| Original and existing data preserved | Original SpaceScan profile remains intact. Its 10,109,036-byte C: history was copied byte-for-byte; both SHA-256 values remain `dfd891a466f1cb1ee779571b255bc36108de771bec4cf4c05c29ddac0357988e`. All 37 pre-existing Diskweft histories still match their inventory hashes. Version acknowledgement was not migrated. |
| History access and provenance | Installed History lists the migrated snapshot; installed and recovered original readers agree on C:, 4,354,532 files / 746,616,532,810 bytes. Diskweft preserves legacy-unknown provenance. No history rewrite/conversion occurred. |
| Concrete recovery | Recovery set retained on G: with exact-source mapping and SHA-256 manifest; all 44 file copies, five registry exports, two shortcuts and original card rechecked. Recovered original scanner executed the real one-file fixture; recovered original snapshot reader loaded history. Written restore procedure preserves newer Diskweft data. A full system reinstall/registry restoration was not exercised or claimed. |
| SpaceScan retired | Exact hash-verified original Inno uninstaller completed with exit 0 and `Removed all? Yes`. Original EXE/install folder, uninstall registration, executable key, all three shell verbs and both original shortcuts are absent. Obsolete original card was separately removed through reviewed transactional cleanup after its recovery hash was checked. No compatibility alias was created. |
| Final reconciliation and cleanup | Read-only `Verify-SystemReplacementState.ps1` passed after cleanup: installed binary/config/routes/card, original/migrated/existing data, recovery hashes, applicable native launch receipts, absence of obsolete routes and exact spent scratch roots. No test application remained running. |

## Execution boundaries

Administrator consent was retained for each action through the persistent broker:
v0.1.1 update request `3974ded715ab40f598a8291465c1beed`, SpaceScan native uninstall
`4dcd353c0fad4a00be81cb9b3ca5fc42`, and reviewed native-staging cleanup
`4c4cb804598147acba7b441a94bda991`. Each actually launched and completed. The initial
0.1.0 installed-profile startup exposed the missing-banner defect; S4 was reopened,
S5 parked, and verified corrected 0.1.1 delivered before S5 resumed.

The uninstall wrapper initially returned 1 after successful native removal when
one provider hash read reported the copied history missing. Immediate independent
existence/hash reads confirmed both histories unchanged. No data restore, copy,
uninstall or installer replay followed. Read-only reconciliation required the
original successful native log and rechecked actual state. The isolated provider
failure's cause was not established; it is not counted as a successful wrapper run.

The first launch probe assumed input-idle implied a main HWND and failed before
observing the WPF window. Its exact child was closed normally. The corrected
probe observes WindowOpened or process exit and verified the actual installed EXE;
no production sleep/retry workaround was introduced.

Pathweft's caller/source/runtime/configuration were left unchanged as expressly
requested. Its integration is an exclusion, not a required deferral. No schedule,
service, background monitor or permanent test server was added.

## Recovery and retained evidence

Intentional recovery location: `G:/Backup/Diskweft/SpaceScan-20261104`, with
`RECOVERY.md`, `files.json`, original install/profile, previous Diskweft profile,
registry inventory, card and shortcuts. The procedure restores exact original
paths through the broker, reviews registry imports and preserves newer data.
No rollback was needed or performed. Published versioned packages remain under
`dist/0.1.0` and `dist/0.1.1`; the upstream comparison baseline remains retained.

Private `.local/s5` contains installation/native-uninstall logs, inventory,
migration/recovery, actual classic/raw-NTFS/EXE launch, retirement and final-state
receipts. Personal paths, reference images and raw diagnostics are not published.
The migration scripts are reviewable, one-time, receipt-bound execution records;
do not repeat them against changed user state. The final-state verifier is read-only
apart from updating its own evidence receipts and creates no scan fixtures.

## Cleanup receipts

| Transaction | Final inspected result |
| --- | --- |
| `0a50d850cf836dabecd22e7efad54f47` | APPLIED: 18 members / 4,492,290 logical bytes; obsolete original card and unpublished/generated candidates only. Historical/current published packages and recovery retained. |
| `3798292eb2651109aca3092964e28e09` | APPLIED: all 63 members / 6,547,082 logical bytes of exact E: delivery/fixture scratch. Signed manifest `172142c198d20cfd35046fb61e416e42d21830e2e1e19957209e201595993c0b` inspected before ticket. Root confirmed absent. |
| `a2820bb6468b4b867fa264a5b8363796` | APPLIED: six members / 4,422,817 logical bytes. Three task-only automatic fixture histories removed; administrator-owned native Inno staging initially reported locked under ordinary token and completed with the same ticket through the broker. Signed manifest `059fb17c79c6594398a12e5366c51df372266af16c6e683b3ab5624047790b4b`. |

Native Inno staged its completed uninstaller under the user's C: Temp despite
task-owned E: scratch. Only that exact log-attributed directory was registered;
no broad C: Temp/profile cleanup or global TEMP change occurred. D: native build
scratch used the documented small-file/native-I/O exception and was cleaned after
guest/process exit. All required retained evidence is outside deletion targets.

S5 closure assessment: migration uses the native installer/uninstaller and
independent Diskweft identities; recovery keeps exact original artifacts instead
of inventing a compatibility layer. No material integration or consolidation gap
remains. The final S0-S5 requirement map is in the implementation plan.
