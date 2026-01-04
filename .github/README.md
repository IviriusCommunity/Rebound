<!-- Title -->
<h1 align="center">
  <span style="display:inline-block; vertical-align:middle;">
    <img width="32" height="32" alt="AppIcon" src="https://raw.githubusercontent.com/IviriusCommunity/Rebound/refs/heads/main/src/system/Rebound.Hub/Assets/AppIcons/Rebound.ico" />
  </span>
  <span style="display:inline-block; vertical-align:middle;">
    <strong>Rebound</strong>
  </span>
</h1>

<!-- Tagline -->
<p align="center">
  <em>Modern Windows done right. Rewritten.</em>
</p>

> [!WARNING]
> The current document contains information for the upcoming Rebound v0.1.0. Some features displayed here might not be available for consumers yet.

Rebound is a Windows modernization project focused on improving the user experience and updating system functionality. It replaces legacy or unmaintained Windows applications with modern implementations built using current technologies. The project aligns with the Windows 11 design direction by extending WinUI to areas of the system that still rely on older frameworks, with the goal of providing a cleaner and more consistent application experience.

Our vision for Windows 11 is a consistent OS with no leftovers and many cross application features. We believe that both consumers and powerusers deserve to have the Windows 11 they love, and we want to provide as much customizability and consistency as possible while keeping it lightweight and easy to use.

<img width="2879" height="1799" alt="image" src="https://github.com/user-attachments/assets/11284d21-4622-44d8-a888-adf3dbfb9206" />

> [!WARNING]
> The project is still in its early stages of development. `v0.0.10` has known issues that will be fixed once `v0.1.0` is released.
> This document is incomplete. Links to our official website may not point to valid locations.

## 👀 Getting started

Using Rebound is a very straightforward process. It's recommended that you have enough knowledge about Windows internals to navigate through this project and debug on your own in case an issue occurs. Windows mods are layers of customization applied over an existing Windows installation and should be handled with care.

> [!WARNING]
> Rebound won't work properly on Windows 10 or debloated systems like Tiny11.

### Installation

After downloading `Rebound Hub Installer.exe`, you will be greeted with the following options:

- Install - this will install Rebound Hub onto your machine, and you will have to enable Rebound separately with each individual mod.
- Uninstall -this will remove both Rebound and Rebound Hub from your machine
- Repair - this will attempt to repair everything that is currently installed from Rebound 

To learn how to uninstall Rebound without using the Rebound Hub Installer, check [Uninstalling Rebound](https://ivirius.com/docs/rebound/uninstalling-rebound/).

### Manual installation

After [Compiling Rebound](https://ivirius.com/docs/rebound/compiling-rebound) or getting the `.msixpackage` and `.cer` from our private testing channels, you can install Rebound Hub in a couple easy steps:

1. Install `Rebound.Hub[ver].cer` by following [this guide](https://ivirius.com/docs/general/install-app-package-manually).
2. Open `Rebound.Hub[ver].msixbundle` and install it 

> In case step two fails because the package is already installed, simply uninstall the existing Rebound Hub and install the new one

### Configuring 

After you're done installing Rebound Hub, you can begin configuring your Rebound installation.

To enable Rebound, open Rebound Hub, go to the `Rebound` tab, and press the button that says `Rebound is not enabled`.

After enabling Rebound, you can install each Rebound app you need. In order to configure them, search in the navigation view or use the search bar to find the dedicated configuration page for each mod.

To change general Rebound settings, use the dedicated settings page inside Rebound Hub. These settings apply to every Rebound app that supports them, or to Rebound as a whole.

## ⚙️ Technical details

Rebound as a whole is a Windows 11 metamod: a collection of mods that rely on a custom runtime and environment to function. Every primary application in Rebound is UWP/WASDK with WinUI 2/3. More specifically, the majority of user applications that are specific to Rebound consist of UWP XAML islands. The installers are WinUI 3, and the wrapper executables and custom DLLs are made on pure Win32. In short terms, the entirety of Rebound is WinUI, except for wrapper executables and custom DLLs.

All first party Rebound mods can be found in this repository. They can be installed via Rebound Hub (the dashboard application for Rebound), which is the only application that can run fully outside the Rebound environment itself.

The core of Rebound consists of multiple mandatory mods that are installed automatically via Rebound Hub when you enable Rebound. These mods are critical for the metamod's functionality and cannot be turned off.

For more information on how Rebound works behind the scenes, check out [Rebound - structure](https://ivirius.com/docs/rebound/structure/).

## 🧰 Apps list

_Included in Rebound:_
- **Disk Cleanup**
- **Character Map UWP**
- **Files**
- **User Account Control Settings**
- **About Windows** (winver)
- **Shell**: (Shutdown dialog, Run)

_Featured in Rebound Hub:_ (optional)
- **Ambie**
- **Character Map UWP**
- **Fairmark**
- **Files**
- **Fluent Store**
- **FluentHub**
- **Fluetro PDF**
- **PowerToys**
- **Scanner**
- **Screenbox**
- **SecureFolderFS**
- **WindowSill**
- **Wino Mail**
- **Wintoys**

## 🛠️ Key Features Comparison

| **Feature**                         | **Classic Windows Mods** | **Rebound** | **Remarks** |
|-------------------------------------|--------------------------|-------------|-------------|
| System-wide customization           | ✔️                       | ❌         | No `msstyles` modifications (no SecureUxThemePatcher used) |
| Fully reversible changes            | ❌                       | ✔️         | No need for system restore points |
| Compatible with all software        | ❌                       | ✔️         | Works with both Win32 APIs and Microsoft Store apps |
| Windows Updates enabled             | ⚠️                       | ✔️         | Updates are enabled by default, ensuring your system remains secure |
| Backwards compatibility             | ❌                       | ✔️         | Win32 system apps remain functional even if Rebound apps malfunction |
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
| **Memory**           | 4 GB                          |
| **Storage**          | 1.5 GB of free disk space (or more, depending on how much of the mod you wish to install) |

## 🛡️ Modding And Security Information

Rebound is built with security and system integrity in mind. It complies with Microsoft's guidelines and does not patch, replace, or tamper with protected system files. Instead, Rebound relies on a custom modding engine that operates through safe, runtime-level techniques such as low-level hooks, DLL injection, Image File Execution Options (IFEO), and resource overrides.

At the core of this system is an abstract instruction layer used to define mod behavior for each Rebound app. These instructions are applied dynamically at runtime, validated for safety, and built to support fallback and error handling when unsupported conditions are detected. This method limits the potential impact on system stability while still allowing meaningful enhancements.

Importantly, Rebound does not rely on third-party patchers like SecureUxTheme or system-wide shell replacements. Every mod is fully reversible and sandboxed to avoid permanent changes or data loss.

> [!NOTE]
> While Rebound is safe for home users and has been tested extensively, no modification method is entirely risk-free. Always back up important data and ensure you're running a supported version of Windows 11 before installing new software.

Rebound has been tested and is compatible with the following security software:

[![Windows Security](https://img.shields.io/badge/Windows%20Security-4466FF?style=flat)](https://www.microsoft.com/windows/comprehensive-security?r=1)
[![ESET](https://img.shields.io/badge/ESET-22BBCC?style=flat)](https://www.eset.com/)
[![Bitdefender](https://img.shields.io/badge/Bitdefender-ff0000?style=flat)](https://bitdefender.com/)
