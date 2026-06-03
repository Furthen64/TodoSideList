# TodoSideList
Sidelist Todo for ubuntu 

## Ubuntu custom shortcut setup (Wayland)
On Ubuntu/Wayland, desktop environments commonly block app-registered global hotkeys, so TodoSideList uses desktop custom shortcuts that launch command mode.

Settings → Keyboard → View and Customize Shortcuts → Custom Shortcuts

Example (adjust path/name):

```bash
dotnet /path/to/TodoSideList.App.dll --hotkey toggle
```

Published app:

```bash
/path/to/TodoSideList.App --hotkey toggle
```
