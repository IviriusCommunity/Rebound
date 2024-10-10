# ℹ️ `Rebound.About`

### 🤔 What is this?

Rebound About (previously ReboundWinver) is Rebound 11's WinAppSdk version of the classic `winver.exe` application.
It has a well thought-out UI and boasts features such as the ability to easily copy your Windows version number to make reporting bugs and contacting support easier since you can instantly provide your Windows and Rebound version to tech support.

### ⚒️ How does it work behind the scenes?

~~_Quickly takes a look to see how it actually works before writing this_~~

Rebound About works by pulling the Windows version and build number from the `Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion` registry key. It also retrives the user email from the same key, under `RegisteredOwner`.

![What a lovely app!](https://github.com/user-attachments/assets/8da9fd20-cdbf-4008-85ca-bca80b633a6b)
