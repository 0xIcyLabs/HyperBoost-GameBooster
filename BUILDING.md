# 🔧 Building HyperBoost from source

HyperBoost is a plain C# WinForms app compiled directly with the .NET Framework compiler — no NuGet, no project files, no Visual Studio required (though both work fine).

## Prerequisites (pick one)

**Option A — Visual Studio / Build Tools (recommended)**
- Install [Visual Studio](https://visualstudio.microsoft.com/) (any edition, with the **.NET desktop development** workload) or the standalone [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022).
- This gives you `csc.exe` via the **Developer Command Prompt for VS**.

**Option B — .NET Framework SDK only**
- Any Windows 10/11 machine already ships `csc.exe` at:
  `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`

## Build

From the repository root (use the Developer Command Prompt, or the full path to `csc.exe`):

```bat
csc.exe /nologo /target:winexe /out:"HyperBoost v1.5.0.exe" ^
  /win32manifest:app.manifest /win32icon:HyperBoost.ico ^
  /r:System.dll /r:System.Core.dll /r:System.Data.dll ^
  /r:System.Drawing.dll /r:System.Windows.Forms.dll ^
  /r:System.ServiceProcess.dll /r:System.Xml.dll *.cs
```

PowerShell variant:

```powershell
csc.exe /nologo /target:winexe /out:"HyperBoost v1.5.0.exe" `
  /win32manifest:app.manifest /win32icon:HyperBoost.ico `
  /r:System.dll /r:System.Core.dll /r:System.Data.dll `
  /r:System.Drawing.dll /r:System.Windows.Forms.dll `
  /r:System.ServiceProcess.dll /r:System.Xml.dll *.cs
```

What each flag does:

| Flag | Purpose |
|---|---|
| `/target:winexe` | Windows GUI app (no console window) |
| `/win32manifest:app.manifest` | Requests **administrator elevation** via UAC |
| `/win32icon:HyperBoost.ico` | Embeds the app icon |
| `/r:*.dll` | Framework references (WinForms, service control, etc.) |

## Regenerate the icon (optional)

`HyperBoost.ico` is committed, but you can regenerate it from the PNG:

```powershell
powershell -ExecutionPolicy Bypass -File make-icon.ps1
```

## Verify your build

1. Run the exe — it should request admin rights (UAC prompt) and show the footer **"HyperBoost v1.5.0 • 0xIcyLabs"**.
2. Check file properties → **Details** tab: Product *HyperBoost*, Copyright *© 2026 0xIcyLabs*.

## Run the test harness (optional)

`test/TestHarness.cs` validates localization completeness (every UI key in all 6 languages), format strings, and helper functions:

```bat
csc.exe /nologo /out:test\test.exe test\TestHarness.cs Texts.cs Tweaks.cs JunkCleaner.cs Boost.cs Support.cs /r:System.dll /r:System.Core.dll
test\test.exe
```

Expected output: `== ALL TESTS PASSED ==` (68 localization keys checked).

## Distributing a release
## Distributing a release

1. Build the exe.
2. Compute the SHA-256 for your release notes:
   ```powershell
   Get-FileHash "HyperBoost v1.5.0.exe" -Algorithm SHA256
   ```
3. Attach the exe (and the hash) to a GitHub **Release** — never commit binaries to the repo.

## Troubleshooting

- **`csc.exe` not found** → use the Developer Command Prompt, or the full Framework path shown above.
- **`CS0006` metadata reference errors** → run from the repo root so `*.cs` expands correctly.
- **Icon/manifest errors** → confirm `HyperBoost.ico` and `app.manifest` are in the current directory.
