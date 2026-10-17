# Nook 1.1 — Noisewater Labs

Your glass edge launcher, with a cozy new identity. Nook is the continuation of Sliver.

## Upgrade from Sliver

1. Quit Sliver from its tray menu.
2. Extract this ZIP to a permanent folder.
3. Run `dist\Nook.exe`.

Your existing shortcuts, lists, monitor position, behavior, and preferences are reused automatically. On first launch Nook applies the new Cozy palette and retains your former colors under **Preferences > Colors > Previous colors**. Later launches keep whatever colors you choose. If startup was already enabled, Nook updates that entry to its new executable location. Startup remains off if it was off before.

For compatibility, settings remain at `%LOCALAPPDATA%\Noisewater Labs\Sliver\settings.json`. Do not delete that folder during the upgrade. Nook and Sliver share a single-instance lock; quit the old version before starting the new one. The former EXE can be removed after the upgrade.

## What's new

- Nook branding and a warm arch logo, with an embedded app/tray icon.
- Cozy espresso and honey-amber palette; soft glass styling and rounded rows.
- Filled selected-list tab and warm line icons for folders and common locations.
- Cleaner shortcut rows: destinations appear as tooltips. Turn the second text line back on in Appearance.
- Search hint: “Find your place…”; Ctrl+K focuses search while Nook is active.
- All existing color controls remain: Cozy, Mint, Ocean, Violet, Amber, Rose, Graphite, custom color picker, hex values, and preview.
- The in-panel logo, tabs, icons, menus, and controls follow the selected colors. The packaged app icon remains amber.

The visual concept guided this implementation. Glass remains translucency rather than Windows desktop blur. Nook does not replace your wallpaper with the concept background.

## Use

Hover on the slim tab at the screen edge. Hold Ctrl for the second list, or click its tab. Add shortcuts with the Add shortcut button, or drag files/folders/apps onto the panel. Right-click a row to edit, reorder, move to the other list, copy its destination, or remove it. Removing shortcuts never deletes their destinations.

| Control | Action |
|---|---|
| Win + Alt + Space | Open/close Nook; focus search when opening |
| Ctrl while panel is open | Temporarily display the second list |
| Ctrl+K while Nook is active | Focus search |
| Enter in search | Launch the first visible result |
| Escape | Close panel |
| Shift + drag the edge tab | Adjust vertical position |
| Pin | Stay open after pointer leaves |
| Tray menu | Open, preferences, pause hover, settings folder, quit |

Supported destinations: full file/folder/app paths, network shares, mapped drives, HTTP/HTTPS websites, shell: locations, and ms-settings: addresses. Environment variables such as `%USERPROFILE%` work. Custom local PNG/JPG/BMP/ICO icons are supported. App arguments are optional. Keep custom images at stable paths: backups contain their paths, not image copies. Network locations are not crawled or polled while opening the menu.

Preferences has Appearance, Colors, Behavior, and Backup & help. Choose the monitor and edge, list names, opacity, hover/hide delays, animation, full-screen suppression, and startup behavior. Save applies changes without restarting. Color previews do not change the current palette until saved. Text and highlights adapt for contrast, so extreme custom colors may be adjusted on screen.

## Data and backups

Nook saves atomically, preserving the prior settings as `settings.json.bak`. Unreadable settings are retained under a timestamped filename, then the backup is tried before falling back to defaults. Preferences supports JSON export/import. Imports do not launch any targets. Only import configurations you trust; shortcuts may point to programs and include arguments.

Errors are written to `errors.log` beside settings. Startup uses the current user's Windows Run entry; it does not require administrator access. To uninstall, turn startup off, quit Nook, and delete its app folder. Retain the settings folder if you want to reinstall later.

## Build

Requires Windows 10/11 with .NET Framework 4.8. Run `BUILD.cmd`; no Visual Studio, Python, CMake, or NuGet is required for that route. It compiles with the framework's included compiler.

- `BUILD.cmd -fast`: no ending pause.
- `BUILD.cmd -run`: build and launch.
- Output: `dist\Nook.exe`.
- Compiler details: `build.log`.

Quit Nook before rebuilding its EXE. Theme, icon, and manifest are embedded. Only the EXE needs to be copied; the framework is supplied by Windows. The optional `Nook.csproj` supports an IDE/SDK build and restores Microsoft's .NET Framework reference package.

## Verification

Compiled against Microsoft's .NET Framework 4.8 references with C# 5 compatibility, with no compiler errors or warnings. Twenty core checks passed, including configuration backups, custom color persistence, upgrade preservation, and idempotent migration. Theme XML and resource references were inspected.

This build environment is Linux. The Windows UI, startup registry migration, Explorer launches, and mixed-DPI placement still require runtime verification on Windows. The included executable is unsigned. The app has no telemetry or background online service.

Optional core checks with .NET 8 SDK: `dotnet run --project Tests\CoreSmoke.csproj`.
