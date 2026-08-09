# 03 — Initial Platform: STEALTHEYELLC

Status: **Canonical live target baseline**
Inspection date: **2026-08-08**
Inspection mode: **read-only machine queries; no desktop automation or test application launch**

## 1. Target machine

```text
computer: STEALTHEYELLC
manufacturer: HP
model: OMEN Gaming Laptop 16-ap0xxx
OS: Microsoft Windows 11 Home x64
DisplayVersion: 24H2
version: 10.0.26100
build: 26100.8973
architecture: x64
CPU: AMD Ryzen 9 8940HX with Radeon Graphics
logical processors: 32
physical RAM: 33,342,455,808 bytes (~33.34 GB decimal)
```

Observed graphics devices:

```text
AMD Radeon(TM) 610M
NVIDIA GeForce RTX 5060 Laptop GPU
```

The initial implementation should exploit this current Windows 11 target while keeping provider boundaries versioned. It should not introduce an artificial old-Windows compatibility floor before a supported target requires one.

## 2. Interactive session reality

The connected machine-control process runs outside the user's interactive desktop context. The live desktop observed read-only was:

```text
interactive user: STEALTHEYELLC\StealthEye
Windows session ID: 1
interactive shell: explorer.exe
```

The normal interactive window station is expected to be `WinSta0`, but the exact station and input desktop were deliberately not probed through UI operation during this pass. This confirms the permanent per-interactive-session boundary. A service or connector process in session 0 cannot be treated as the desktop operator merely because it can enumerate machine state. Build 001's Session Host must execute in session 1 under the interactive user, discover its actual window station/input desktop, and report lock or inaccessible-desktop state rather than attempting to bypass it.

The inspection did not open, focus, move, capture, click, type into, or automate any desktop application.

## 3. Operating-system capability consequences

Build 26100 supports the frozen public-API spine:

- direct native COM UI Automation interfaces through the current UIA generation;
- UIA6 event-handler groups where interface probing succeeds;
- USER32, DWM, WTS, WinEvent, window-station and desktop APIs;
- Windows Graphics Capture with HWND interop;
- per-monitor DPI and current virtual-desktop coordinate APIs;
- local named pipes and SQLite WAL.

Interface availability must still be probed at runtime. An OS build number is not permission to assume that every application provider behaves correctly or exposes every UIA pattern.

## 4. Display and coordinate baseline

The same-date live inventory observed the current internal display at:

```text
resolution: 1920 × 1200
refresh rate: 144 Hz
GPU topology: hybrid AMD integrated + NVIDIA discrete
```

This is a measurement, not a persistent coordinate contract. Build 001 must query the current monitor set, virtual origin, rotation, DPI and transforms and advance `DisplayTopologyEpoch` on change. It must be correct with negative virtual coordinates and must never persist a click point as target identity.

## 5. .NET

Observed:

```text
dotnet: C:\Program Files\dotnet\dotnet.exe
SDK: 10.0.302
Microsoft.NETCore.App: 10.0.10
Microsoft.AspNetCore.App: 10.0.10
Microsoft.WindowsDesktop.App: 10.0.10
```

This supports the frozen C#/.NET 10 x64 kernel, Session Host, workers and WPF fixture choice. Direct COM/Win32 interop remains the UIA/native foundation; the WindowsDesktop runtime does not make `System.Windows.Automation` the architectural provider.

## 6. Node.js and npm

Node is available as a portable runtime rather than through the machine-wide PATH:

```text
C:\AgentBrowser\tools\node-v24.18.1-win-x64\node.exe
Node: v24.18.1
npm: 11.16.0
```

Build 001 should invoke this runtime explicitly. It must not modify the global PATH merely to host the disposable Node 24 Program Host.

## 7. Git and PowerShell

Observed:

```text
Git: 2.55.0.windows.3
Windows PowerShell: 5.1.26100.8972
powershell.exe: available
pwsh.exe: not found on the machine-control SYSTEM PATH
```

PowerShell is not a DESKTOPeye provider or Program Host. It may be used by implementation tooling, but Milestone D cannot pass through a giant PowerShell/UIA macro.

## 8. Relevant installed applications

The same-date platform inventory found current Google Chrome and Microsoft Edge installations. They are useful later for native-frame, Electron/Chromium, file-picker and cross-substrate smoke tests. Browser content semantics remain eyeBROWSE-owned.

Build 001's first real-world post-gate target is the Windows Common Item Dialog initiated from Notepad Save As. The architecture pass did not launch Notepad, a dialog, a browser, Calculator, Explorer, Settings, VS Code, or either future fixture.

## 9. Storage and runtime placement

Observed local filesystems include:

```text
C: NTFS
X: ReFS
```

The exact DESKTOPeye runtime layout is an implementation decision. Required placement rules are:

- operating SQLite, pipe discovery, capture scratch and worker-spool state live outside the Git repository;
- product repositories contain source and deterministic fixtures only after implementation is authorized;
- frames and screenshots remain ephemeral and are not accumulated as operating history;
- runtime paths are explicit, versioned and user/session appropriate.

## 10. Build 001 implications

The verified platform supports the candidate stack without installing another language or database. The implementation pass should begin with:

- C#/.NET 10 x64;
- direct native COM UIA plus managed USER32/DWM/WTS/WinEvent/WGC interop;
- SQLite WAL;
- Windows named pipes;
- the existing portable Node 24 runtime;
- one interactive-user Session Host in session 1.

No C++, Rust, Python, PowerShell 7, driver, DLL injection, service, UIAccess deployment, OCR package, RPA framework, or graph database is a prerequisite.

## 11. Verification boundary

These are platform facts at one observation time. They do not prove UIA provider quality, event completeness, WGC minimized/protected behavior, input targeting, HWND reuse behavior, worker isolation, virtualized-item identity, or recovery. Those remain the explicit Build 001 experiments and acceptance gates in `02-BUILD-001-SLICE.md`.

```text
Product implementation: NOT STARTED
Build 001 acceptance:   NOT RUN
```
