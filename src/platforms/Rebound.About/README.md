# ℹ️ Rebound About

Replacement for: **winver**

## Overview

**Rebound About** is a modern replacement for *winver* (About Windows), built with WinUI 3 to match the Windows 11 design language. It replicates the core functionality of the original dialog and introduces improvements, including copyable Windows version information, support for SVG-based banners, and additional version details for Rebound itself.

* * *

## Technology Stack

* **Framework:** Windows App SDK
* **UI:** WinUI 3
* **.NET Version:** 9.0
* **Target Windows Version:** 24H2
* **Packaging:** Self-contained single executable

* * *

## Public APIs

| Name | Type | Description |
| --- | --- | --- |
| `rwinver.exe` | App | The winver application (`C:\Program Files\Rebound\rwinver.exe`) |
| `legacy` | Argument | Launches the original winver when Rebound About is installed |