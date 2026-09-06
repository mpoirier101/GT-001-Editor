# Releasing

This is a simple manual release checklist for GitHub.

## Before Tagging

1. Build and test from a clean checkout.
2. Verify the app connects to the GT-001.
3. Verify patch selection, parameter editing, and WRITE using an expendable User patch slot.
4. Confirm the repository does not include `reference/`, vendor manuals, local logs, screenshots, or capture files.

## Build a Release Folder

```powershell
dotnet restore GT001.Editor.sln --configfile NuGet.Config
dotnet test GT001.Editor.sln --no-restore
dotnet publish src\GT001.Editor.App\GT001.Editor.App.csproj -c Release -r win-x64 --self-contained false -p:DebugType=none -p:DebugSymbols=false -o artifacts\release\GT001.Editor-vX.Y.Z-win-x64
```

Upload the installer produced below to the GitHub release. The framework-dependent publish folder is an intermediate build artifact, not a release asset.

## Build the Windows Installer

Install [Inno Setup](https://jrsoftware.org/isinfo.php) on the release machine, then run:

```powershell
.\scripts\build-installer.ps1
```

The script publishes a framework-dependent build and produces `artifacts\installer\GT001.Editor-Setup-X.Y.Z-win-x64.exe`. The target machine must have the .NET 10 Windows Desktop Runtime installed. The installer requires elevation because it installs to `C:\Program Files\GT-001`; it preserves `%LocalAppData%\GT-001` on upgrades and uninstall.

For local installs, copy the release folder contents to `C:\Program Files\GT-001`. The app install folder is only a publish destination; builds should run from the source checkout. User settings and logs belong in `%LocalAppData%\GT-001`, never in the install folder.

## Release Notes Template

```markdown
## GT-001 Editor vX.Y.Z

### Highlights

- 

### Hardware Notes

- Tested with BOSS GT-001 over USB MIDI.
- GT-001 MIDI receive channel should be set to channel 1.
- Use WRITE only with User patch slots you are willing to overwrite.

### Known Limitations

- SYSTEM/MIDI/USB device settings are edited on the GT-001 itself.
- Patch librarian/import/export workflows are not implemented.
```
