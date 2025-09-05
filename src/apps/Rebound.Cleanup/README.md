# ℹ️ Rebound Disk Cleanup

Replacement for: **cleanmgr**

## Overview

**Rebound Disk Cleanup** is a modern replacement for _cleanmgr_ (Disk Cleanup), offering an extended set of cleaning options and improved performance. In addition to traditional system cleanup, it includes support for browser data removal, jump list cleanup, and delivers a unified, streamlined user experience.

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
| `rcleanmgr.exe` | App | The winver application (`C:\Program Files\Rebound\rwinver.exe`) |
| `legacy` | Argument | Launches the original winver when Rebound About is installed |
| `clean` | Argument | Launches the app in the background to perform a cleaning task on all drives for the selected items |