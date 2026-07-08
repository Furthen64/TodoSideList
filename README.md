# TodoSideList
Sidelist Todo for Ubuntu.

## Ubuntu custom shortcut setup (Wayland)
On Ubuntu/GNOME Wayland, TodoSideList cannot reliably register `Super+T` as a
global hotkey by itself. The `Super+T` label in the app shows the intended
shortcut, but pressing it after hiding the window only works after GNOME owns
that shortcut and launches TodoSideList command mode.

If you start the app from this source checkout with:

```bash
./launch.sh
```

Create a GNOME custom shortcut:

```text
Settings -> Keyboard -> View and Customize Shortcuts -> Custom Shortcuts
```

Use these values:

```text
Name: TodoSideList Toggle
Command: /home/fat64/github/TodoSideList/launch.sh --hotkey toggle
Shortcut: Super+T
```

Adjust the command path if your checkout is somewhere else.

For an installed/published copy, point the shortcut at the app executable
instead:

```bash
/path/to/TodoSideList.App --hotkey toggle
```

For a DLL-based launch:

```bash
dotnet /path/to/TodoSideList.App.dll --hotkey toggle
```

You can test the toggle command manually while the app is running:

```bash
/home/fat64/github/TodoSideList/launch.sh --hotkey toggle
```

If that command works but `Super+T` does nothing, the GNOME custom shortcut is
missing, points to the wrong command, or conflicts with an existing system
shortcut.
