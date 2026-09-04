# ⚡ HyperBoost — Game Booster

> One-click **service control**, **RAM cleanup** and **anti-stutter boost** for gaming on Windows 10 / 11.
> Fully multilingual — English, 日本語, 한국어, 中文, Français, العربية (with right-to-left support).

![badge-win](https://img.shields.io/badge/Windows-10%20/%2011-00e8b3) ![badge-lang](https://img.shields.io/badge/Languages-6-00e8b3) ![badge-free](https://img.shields.io/badge/Free-✓-00e8b3) ![badge-src](https://img.shields.io/badge/Source-open-00e8b3)

**Publisher:** 0xIcyLabs · **License:** MIT · **Version:** 1.5.1

---

## 📸 Screenshot

![HyperBoost — main window](sample.png)

---

## ✨ Features

| | Feature | What it does |
|---|---|---|
| 🛠 | **Service control** | Curated grid of **57 services** commonly disabled for gaming — only ones installed on *your* PC are shown, running ones first. |
| 🚀 | **OPTIMIZATION** | One click: stops all configured services in parallel, flushes RAM, reports exactly how much was freed. |
| ⚡ | **PERFORMANCE BOOST** | Anti-stutter pack in one pass — power plan, GPU persistence, timer resolution, RAM monitor. |
| ↩️ | **REVERT BOOST** | Reverts everything BOOST changed — automatically on exit too. |
| 🧹 | **JUNK CLEANUP** | Windows Temp, user temp, Windows Update cache. |
| 🎮 | **GAME AGENT** | Auto-boost when a game starts, auto-revert when you leave. Hotkey **Ctrl + Alt + B**, tray icon. |
| 🛡️ | **RECOVERY** | One-click restore after a crash or force-kill. |
| 🧩 | **6 GAMING TWEAKS** | Game Mode, Game DVR, network QoS, foreground priority, PCIe/USB/CPU power — all reversible. |
| 🌍 | **6 languages** | Switchable anytime from the top-right menu. |

➡️ Full details and step-by-step instructions: **[USAGE.md](USAGE.md)**

---

## 🚀 Quick start

1. **Download** [`HyperBoost v1.5.1.exe`](releases/latest).
2. **Run it** — accept the UAC prompt (administrator rights are required).
3. **No installation needed** — runs portable. Data lives in `%LOCALAPPDATA%\HyperBoost`.

## ⚠️ SmartScreen & antivirus

The executable is **currently unsigned**, so Windows SmartScreen and some antivirus engines may warn. Click **More info → Run anyway**, or review the open source and build it yourself from this repo — see [BUILDING.md](BUILDING.md).

## 🔐 Verify your build

The program is fully open source — review the code and compile it yourself, see [BUILDING.md](BUILDING.md). SHA-256 hashes of release executables are published with each GitHub Release so you can verify your download.

## 💻 Requirements

- Windows 10 or 11 (x64)
- Administrator rights

## 🛡️ Safety

Fully reversible · documented Windows APIs only (`powercfg`, `nvidia-smi`, service/SDK APIs) · no telemetry · no network access · never disables antivirus or force-kills tasks. Details in [USAGE.md](USAGE.md#️-safety--privacy).

## 📦 What's in this repository

```
HyperBoost/
├─ Program.cs              # main window, service engine, auto-agent, tray
├─ Boost.cs                # power plan / GPU / timer-resolution boost + revert
├─ Memory.cs               # RAM measurement, cleaner, monitor
├─ Tweaks.cs               # 7 reversible gaming tweaks + recovery journal
├─ TweaksForm.cs           # tweaks checklist UI
├─ JunkCleaner.cs          # temp + update-cache measurement & cleanup
├─ JunkForm.cs / JunkRow.cs# junk-cleanup dialog + animated rows
├─ Ui.cs                   # design system (palette, spacing, tile buttons)
├─ OptimizationButton.cs   # animated hero button
├─ Support.cs              # game agent, recovery state, config
├─ Texts.cs                # 6-language localization table
├─ AssemblyAttrs.cs        # version / publisher metadata
├─ app.manifest            # admin elevation
├─ make-icon.ps1           # regenerates HyperBoost.ico
├─ README.md               # this file
├─ USAGE.md                # user guide
├─ BUILDING.md             # build-your-own instructions
├─ LICENSE                 # MIT © 0xIcyLabs
└─ .gitignore
```

**Source only** — release binaries are published on the [Releases](releases/latest) page, not committed here.

## 🛠 Build it yourself

One command, no dependencies beyond the .NET Framework compiler:

```powershell
csc.exe /nologo /target:winexe /out:"HyperBoost v1.5.1.exe" `
  /win32manifest:app.manifest /win32icon:HyperBoost.ico `
  /r:System.dll /r:System.Core.dll /r:System.Data.dll `
  /r:System.Drawing.dll /r:System.Windows.Forms.dll `
  /r:System.ServiceProcess.dll /r:System.Xml.dll *.cs
```

Full walkthrough: **[BUILDING.md](BUILDING.md)**

## 📝 Release notes

**v1.5.1**
- Added the **Game Agent** (auto-boost / auto-revert, tray, `Ctrl+Alt+B`).
- Added **RECOVERY** (self-healing after crash / force-kill) and persisted undo journal.
- Added **session reports** and the **6th tweak** (QoS packet priority).
- Publisher: **0xIcyLabs** — shown in the app footer.

**v1.4.0**
- Added **GAMING TWEAKS** (reversible registry + power tweaks).
- Reworked the service engine: dependency-aware stops, parallel execution, disabled-service detection, friendly localized failure reasons.

---

© 2026 **0xIcyLabs** · MIT License
