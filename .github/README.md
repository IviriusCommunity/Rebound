![image](https://github.com/user-attachments/assets/e7233bd3-710c-45d7-a50e-20c3ff234be2)

<!--<p align="center">
  <a style="text-decoration:none" href="https://github.com/IviriusCommunity/ReboundHub/actions/workflows/ci.yml">
    <img src="https://github.com/IviriusCommunity/ReboundHub/actions/workflows/ci.yml/badge.svg" alt="CI Status" /></a>
  <a style="text-decoration:none" href="https://dsc.gg/ivirius">
    <img src="https://img.shields.io/discord/1137161703000375336?label=Discord&color=7289da" alt="Discord" /></a>
</p>-->

**Modernizing Windows 11 the right way.** Rebound is a comprehensive enhancement project designed to modernize Windows 11 by replacing legacy Win32 applications with consistent, native WinUI 3 alternatives. It delivers a unified experience without modifying system files, ensuring proper stability, compatibility, and security.

> [!WARNING]
> The project is still in its early stages of development and the `ALPHA` versions are considered unstable. A Developer Preview version will be released soon with major improvements.

---

## 🤔 General Information

Rebound is a modern enhancement layer for Windows 11, combining a suite of WinUI 3 applications into a unified modding experience. Instead of modifying system files or using intrusive patches, Rebound offers standalone, first-party apps that integrate cleanly with the operating system.

All official Rebound apps are included in this repository and can be installed individually using Rebound Hub, a dedicated app that manages the entire ecosystem. Each app is built to function independently, but together they form a consistent and future-friendly alternative to legacy Windows components.

> [!NOTE]
> Rebound uses its own lightweight mod runtime, built on low-level hooks, Image File Execution Options (IFEO), and icon overrides to replace legacy Win32 apps in a non-invasive way. It **does not** patch or modify system files, ensuring maximum compatibility and safety.

## 🖼️ Screenshots

Thoughtfully designed light mode | Thoughtfully designed dark mode
---|---
![Thoughtfully designed light mode](https://github.com/user-attachments/assets/d87e9fc1-fe1c-461a-a128-6e970b45d9a0)|![Thoughtfully designed dark mode](https://github.com/user-attachments/assets/b578a82d-6386-46cf-b395-0f98e75fbb8a)

## 🎁 Getting started

1. **Download Rebound Hub** online [on GitHub](https://github.com/IviriusCommunity/ReboundHub/releases/latest) (download `ReboundHubInstaller.exe`).
2. **Run the Installer**: Follow the on-screen instructions to install Rebound Hub.
3. **Launch Rebound Hub** : go to `Rebound`, press `Enable Rebound`, and start exploring!

## 🧰 Apps list

_Included in Rebound:_
- **Defragment and Optimize Drives**
- **Disk Cleanup**
- **Character Map UWP**
- **Files**
- **Ivirius Text Editor**
- **About Windows** (winver)
- **MMC** (TPM, Task Scheduler)
- **Shell**: (Desktop, Shutdown dialog, Run, Control Panel)

_Can be installed via Rebound Hub:_ (optional)
- **Ambie**
- **Wino Mail**
- **Screenbox**
- **FluentHub**

_Featured:_
- **Rectify11**

## 🛠️ Key Features Comparison

| **Feature**                         | **Classic Windows Mods** | **Rebound** | **Remarks** |
|-------------------------------------|--------------------------|-------------|-------------|
| System-wide customization           | ✔️                       | ❌         | No `msstyles` modifications (no SecureUxThemePatcher used) |
| Fully reversible changes            | ❌                       | ✔️         | No need for system restore points |
| Compatible with all software        | ❌                       | ✔️         | Works with both Win32 APIs and Microsoft Store apps |
| Windows Updates enabled             | ⚠️                       | ✔️         | Updates are enabled by default, ensuring your system remains secure |
| Backwards compatibility             | ❌                       | ✔️         | Win32 system apps remain functional even if Rebound 11 apps malfunction |
| Safe for home users                 | ❌                       | ✔️         | While no mod is 100% safe, Rebound is generally safer than most mods |
| Open source                         | ⚠️                       | ✔️         | Fully open-source, including dedicated Rebound apps |
| Additional features over Win32 apps | ⚠️                       | ✔️         | Rebound apps offer enhanced features compared to legacy Win32 applets |

## 🎛️ Minimum Requirements

> [!IMPORTANT]
> Ensure your system meets the following minimum specifications before installing Rebound:

| **Component**        | **Minimum Requirement**       |
|----------------------|-------------------------------|
| **Operating System** | Windows 11                    |
| **Version**          | Build 22000 or higher         |
| **Processor**        | 2 GHz or faster, 64-bit CPU   |
| **Memory**           | 8 GB                          |
| **Storage**          | 256 MB of free disk space (or more, depending on how much of the mod you wish to install) |

## 🛡️ Modding And Security Information

Rebound is built with security and system integrity in mind. It complies with Microsoft's guidelines and does not patch, replace, or tamper with protected system files. Instead, Rebound relies on a custom modding engine that operates through safe, runtime-level techniques such as low-level hooks, Image File Execution Options (IFEO), and resource overrides.

At the core of this system is an abstract instruction layer used to define mod behavior for each Rebound app. These instructions are applied dynamically at runtime, validated for safety, and built to support fallback and error handling when unsupported conditions are detected. This method limits the potential impact on system stability while still allowing meaningful enhancements.

Importantly, Rebound does not rely on third-party patchers like SecureUxTheme or system-wide shell replacements. Every mod is fully reversible and sandboxed to avoid permanent changes or data loss.

> [!NOTE]
> While Rebound is safe for home users and has been tested extensively, no modification method is entirely risk-free. Always back up important data and ensure you're running a supported version of Windows 11 before installing new software.

Rebound has been tested and is compatible with the following security software:

[![Windows Security](https://img.shields.io/badge/Windows%20Security-4466FF?style=flat)](https://www.microsoft.com/windows/comprehensive-security?r=1)
[![ESET](https://img.shields.io/badge/ESET-22BBCC?style=flat)](https://www.eset.com/)
