# Rebound About

Replacement for: **winver.exe**

## Overview

Rebound About is a modern alternative to the classic About Windows (winver.exe). It displays the same system version details found in the original and some additional information about Rebound. This lightweight tool reintroduces a familiar utility with a fresh look and expanded functionality.

---

## Technical specifications

Target platform: Windows 11, version 24H2 (10.0.26100.38)
SDK: Windows App SDK v1.7 stable - WinUI 3
Framework: .NET 9.0
Architecture: win-x64 Release
Packaging: self contained single exe

## Public APIs

| Name | Type | Description |
|------|------|-------------|
| rwinver.exe | App | Rebound About application (%PROGRAMFILES%\Rebound\rwinver.exe) |
| legacy | Argument | Launch the legacy winver while Rebound About is installed |
