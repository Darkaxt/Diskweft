Diskweft 0.1.0 portable package

Extract every file together and run Diskweft.exe on Windows 11 with its built-in
.NET Framework. Keep Diskweft.exe.config beside the executable for per-monitor DPI.

Portable means no installation is required. Settings and history still use
%APPDATA%\Darkaxt\Diskweft; they are independent of SpaceScan's profile.
The application publishes its own discovery registration/card when run. Explorer
context-menu integration is optional in Settings. No background service is added.

Whole-volume NTFS/MFT scanning requires administrator rights. Ordinary folder and
network scans use classic traversal without elevation. Partial results describe
discovered bytes, not estimated completion. Unresolved live records are withheld.

Command-line examples:
  Diskweft.exe D:\ --report D:\report.html
  Diskweft.exe C:\Users --csv users.csv --exclude "node_modules;*.iso"

Binaries are unsigned. SHA256SUMS.txt accompanies the published packages.
Source, releases and attribution: https://github.com/Darkaxt/Diskweft
Forked from limburatorul/spacescan; MIT notice is included in LICENSE.
