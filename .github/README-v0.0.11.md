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

Rebound is a Windows 11 modernization project focused on bringing WinUI everywhere in the operating system by replacing legacy or unmaintained applications with modern, native implementations built using WinUI and Fluent Design.

Our vision for Windows 11 is a consistent OS with modern apps everywhere, extended customization, and additional features for both convenience and control. We believe that consumers and powerusers alike deserve to have a consistent operating system, and we want to provide as much customizability and consistency as possible while keeping it fast, lightweight, native, safe, and easy to use for everyone.

<img width="2277" height="1029" alt="Rebound cover art" src="https://github.com/user-attachments/assets/4923da9c-0650-405f-9750-71899a861e28" />

## 💻 System requirements

To install Rebound, first make sure your system meets the following requirements:

- Windows 11 **Build 22000 or higher** (Recommended: Windows 11 26H2)
- **x64/ARM64 processor** (Intel, AMD, Snapdragon, etc.)
- **500 MB of free disk space** or more

> [!WARNING]
> Rebound won't work properly on Windows 10, debloated systems (Tiny11, AtlasOS, etc.), or Linux with Wine/Proton due to missing dependencies and unimplemented APIs.

## 📦 Installing

1. [Download the latest Rebound Installer](https://github.com/IviriusCommunity/Rebound/releases/latest)
2. Run the installer and follow the instructions
3. Go to the `Rebound` tab inside Rebound Hub, then enable Rebound, and install your mods of choice

> [!IMPORTANT]
> To uninstall Rebound, you can either do so **from within Rebound Hub** (recommended) or from `Settings -> Apps`. **Uninstalling Rebound Hub will not remove Rebound from your system.**

## 💼 Built-in Apps
<table>
  <tr>
    <th><a href="https://github.com/IviriusCommunity/Rebound/tree/main/src/apps/Rebound.About">Rebound About</a></th>
    <th><a href="https://github.com/IviriusCommunity/Rebound/tree/main/src/apps/Rebound.ControlPanel">Rebound Control Panel</a></th>
    <th><a href="https://github.com/IviriusCommunity/Rebound/tree/main/src/apps/Rebound.Cleanup">Rebound Disk Cleanup</a></th>
  </tr>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/c88df85c-3cd2-4bc8-badc-1026c1ca2bcf"/></td>
    <td><img src="https://github.com/user-attachments/assets/c5565451-0fd0-4294-b6ec-e8f4df8f247f"/></td>
    <td><img src="https://github.com/user-attachments/assets/597a768c-c7af-432c-9aed-eb31b68be145"/></td>
  </tr>
</table>

## 🧰 Featured Apps

<table>
  <tr>
    <th><a href="https://apps.microsoft.com/detail/9wzdncrdxf41">Character Map UWP</a></th>
    <th><a href="https://files.community/">Files</a></th>
  </tr>
  <tr>
    <td><img width="979" height="779" alt="Character Map UWP" src="https://github.com/user-attachments/assets/65c9acc1-3d0b-483f-a34d-1a9a2278623f"/></td>
    <td><img width="950" height="667" alt="Files" src="https://github.com/user-attachments/assets/a4979060-d699-432c-a5f3-195b4334b3b2"/></td>
  </tr>
</table>

## ✨ Sideloaded Mods

Rebound supports **sideloaded mods**, created by developers without having to add them to the project's codebase. If you wish to create a Windows mod managed by Rebound, this feature is for you. These mods appear under their own section inside Rebound Hub and do not support the full capability set of built-in mods for security reasons. To get started, check out [this guide](https://ivirius.com/docs/rebound/sideloaded-mods/get-started) on our website.

## 🛡️ Security Information

Rebound has been tested and is compatible with the following security software:

[![Windows Security](https://img.shields.io/badge/Windows%20Security-4466FF?style=flat)](https://www.microsoft.com/windows/comprehensive-security?r=1)

## ⚙️ Technical Details

Rebound is a Windows 11 metamod: a collection of mods that rely on a custom runtime and environment to function.

Instead of modifying or replacing system files, Rebound uses DLL injection and API hooking to change the behavior of Windows as it runs. This means every change is immediately reversible and isn't affected by Windows updates.

Every primary application in Rebound is built on WinUI and .NET, while every DLL for injection is built with [MinHook-Detours](https://github.com/m417z/minhook-detours) in C++.

All first party Rebound mods can be found in this repository. They can be installed via Rebound Hub, which is currently the only frontend for Rebound.

The core of Rebound consists of multiple mandatory mods that are installed automatically via Rebound Hub when you enable Rebound. These mods are critical for the metamod's functionality and cannot be turned off.

## 🙋 Got feedback?

We’d love to hear from you! If you want to report an issue or share suggestions, reach out to us via:

* Our [official website contact page](https://ivirius.com/contact/)
* Email: [ivirius.helpdesk@gmail.com](mailto:ivirius.helpdesk@gmail.com)
* [Discord server](https://ivirius.com/discord/)

---

<img width="400" alt="Zippy" src="https://github.com/user-attachments/assets/c1b73b22-37fc-49b8-b0f6-9a3f417b34a3" />
