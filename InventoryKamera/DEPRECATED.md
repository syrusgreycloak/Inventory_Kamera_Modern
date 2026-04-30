# InventoryKamera (WinForms) — DEPRECATED

**Status:** Deprecated as of Milestone 2.

This Windows Forms project is being retired in favor of the cross-platform Avalonia UI.

## Use the Avalonia version instead

The replacement is at `../InventoryKamera.Avalonia/`. Build and run it with:

```bash
dotnet build ../InventoryKamera.Avalonia/InventoryKamera.Avalonia.csproj -c Debug
../InventoryKamera.Avalonia/bin/Debug/net8.0-windows/InventoryKamera.Avalonia.exe
```

## What's still supported here

- Critical bug fixes will be backported during the beta window.
- The WinForms binary still scans correctly.

## Removal date

This folder will be removed in Milestone 2.2 (chunk 12 of the milestone plan) once the Avalonia version reaches feature parity and is validated by community testing.
