# Diskweft

A Windows disk-usage explorer forked from
[SpaceScan](https://github.com/limburatorul/spacescan), with progressive scan
results and Windows 11 appearance. The name complements
[Pathweft](https://github.com/Darkaxt/Pathweft), a separate file-manager project.

**Development status:** S0-S3 are COMPLETE. Independent identity and progressive
classic/MFT scanning are implemented and verified, including real elevated NTFS.
Windows themes, accent settings and native layout verification passed;
see the [S3 evidence](docs/verification/S3-WINDOWS-APPEARANCE.md).
S4-S5 are NOT STARTED. The internal build is `0.0.3-stage3-development`; there is no
Diskweft product release yet. S4 owns final delivery; S5 owns the planned system
replacement of installed SpaceScan after that verified release.

- [Authoritative specification](docs/SPECIFICATION.md)
- [Staged plan and current reconciliation](docs/IMPLEMENTATION-PLAN.md)
- [Development and baseline verification](docs/DEVELOPMENT.md)

The planned outcome includes live folder sizes, file counts and statistics for
both ordinary traversal and NTFS/MFT scans, stable navigation during scanning,
honest partial-result states, and restrained Windows light/dark themes with accent
support. Existing search, duplicate detection, history, reports and file actions
remain compatibility requirements.

Classic scans now publish live folder totals/statistics, allow navigation while
running and preserve partial/cancelled provenance. See the
[S1 verification and native rendering](docs/verification/S1-CLASSIC-LIVE.md).
MFT scans now also publish resolved folders during reading; see the
[S2 real-volume verification](docs/verification/S2-PROGRESSIVE-MFT.md).
Build with `scripts/Build-Development.ps1`; current development output is isolated
from SpaceScan. Do not use the inherited instructions below as a fork release
procedure.

The source baseline is SpaceScan 1.4.1, commit
[`313271a`](https://github.com/limburatorul/spacescan/commit/313271a0bb34e63c8182c49e9c71caabbd782a4a).
It uses WPF and .NET Framework. `build.cmd` now builds the independent Diskweft
development executable; installer packaging and installation acceptance belong
to S4.

MIT licensed. Preserve the original Protagonist Labs notice in [LICENSE](LICENSE).

## Inherited upstream documentation

The following describes SpaceScan's baseline behavior, not completed Diskweft
enhancements. Its installer, integration and product links still target upstream.

# SpaceScan

Finds what is filling up a Windows drive. Free, no limits, no nagging: it reads the NTFS Master File
Table directly, so a whole drive — including a 10 TB one — is mapped in seconds.

![SpaceScan](docs/screenshot.png)

## What it does

- **Folder tree** with a size bar, percentage of the parent folder and the file count on every row.
- **Whole drives in seconds.** On an NTFS volume it reads the MFT (like the fast commercial tools).
  Anywhere else — a folder, a network share, a non-NTFS disk — it walks folders instead, with no
  path-length limit and without following junctions.
- **File types and categories:** where the space goes per extension, or grouped into Video, Photos,
  Archives, Installers and the rest.
- **Largest files** on the drive, and a **search** by name, minimum size and age that also runs off
  the MFT.
- **Duplicates:** same size, then the same hash of the first 64 KB, then of the whole file, with the
  space you would get back.
- **Compare** a scan with an earlier one and see what grew. Every scan is stored automatically.
- **Delete, copy or move** straight from the results, several items at once, through the Windows
  dialogs you already know.
- **Export** to HTML or CSV, and a **command line** for scheduled reports.
- **Exclusions** by name, extension or path, kept between runs.
- **Works with Shelf and Backup Labs** when they are installed: a game's folder is labelled with the
  game, and a game Shelf can move has "Move this game to another drive (Shelf)" in its row menu, which
  opens Shelf's own move; Backup Labs' folders are labelled too, and a folder can be backed up from there.

## Command line

Runs without a window, for Task Scheduler and scripts:

```
SpaceScan.exe D:\ --report D:\report.html --compare-last
SpaceScan.exe C:\Users --csv users.csv --exclude "node_modules;*.iso"
SpaceScan.exe --help
```

Reading the MFT needs administrator rights; in Task Scheduler tick "Run with highest privileges".
Given just a path, SpaceScan opens its window and scans there — that is what the Explorer and File
Labs right-click entries use.

## Requirements

Windows 7 or later, including Windows Server. Nothing to install alongside it: the app is built
against the .NET Framework that ships with Windows.

## Building

```
build.cmd            compiles SpaceScan.exe
.\build.ps1          compiles, draws the icon and builds the installer (needs Inno Setup 6)
.\check.ps1          self-checks; run it elevated to cover the MFT paths too
```

## Licence

MIT — see [LICENSE](LICENSE).

## More from Protagonist Labs

- [Shelf](https://protagonistlabs.app/shelf/?utm_source=github&utm_medium=readme&utm_campaign=spacescan): every PC game in one library, with the size of each install.
- [File Labs](https://protagonistlabs.app/filelabs/?utm_source=github&utm_medium=readme&utm_campaign=spacescan): a free dual-pane file manager.
- [All apps](https://protagonistlabs.app/?utm_source=github&utm_medium=readme&utm_campaign=spacescan): Windows apps that each do one job properly.
