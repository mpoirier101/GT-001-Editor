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
dotnet publish src\GT001.Editor.App\GT001.Editor.App.csproj -c Release -r win-x64 --self-contained true -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:DebugType=none -p:DebugSymbols=false -o artifacts\release\GT001.Editor-vX.Y.Z-win-x64
```

Zip the contents of the release folder for the GitHub release.

For local installs, copy the release folder contents to `C:\Apps\GT-001`. The app install folder is only a publish destination; builds should run from the source checkout.

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
