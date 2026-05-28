# Releasing

This is a simple manual release checklist for GitHub.

## Before Tagging

1. Build and test from a clean checkout.
2. Verify the app connects to the GT-001.
3. Verify patch selection, parameter editing, and WRITE using an expendable User patch slot.
4. Confirm the repository does not include `reference/`, vendor manuals, local logs, screenshots, or capture files.

## Build a Release Folder

```powershell
dotnet publish src\GT001.Editor.App\GT001.Editor.App.csproj -c Release -r win-x64 --self-contained false -o publish\GT001.Editor
```

Zip the contents of `publish\GT001.Editor` for the GitHub release.

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
