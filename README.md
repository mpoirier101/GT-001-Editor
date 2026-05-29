# GT-001 Editor

Unofficial Windows desktop editor for the BOSS GT-001 guitar effects processor.

GT-001 Editor focuses on the everyday patch workflow: select a patch, edit the temporary patch buffer over USB MIDI, and write the result back to a User patch slot when you are ready.

<img width="1916" height="1293" alt="image" src="https://github.com/user-attachments/assets/713ba7f3-2582-4572-8fa6-37e253eed12c" />


## Status

This project is a public beta. It has been tested against real GT-001 hardware, but it is still young software that sends MIDI SysEx write messages to your device.

Working areas:

- USB MIDI connection and GT-001 identity/sync.
- User and Factory patch browser with patch names.
- Device-originated patch change detection when `SYSTEM: MIDI SETTING: PC OUT` is enabled.
- Effect chain display and positioning.
- Type-aware parameter panels for the main patch effects.
- Temporary patch editing.
- WRITE flow for saving the current temporary patch to a chosen User patch number and name.

Currently out of scope:

- SYSTEM, MIDI, USB, and other mostly set-once device settings.
- Patch librarian/import/export workflows.
- Editing quick settings and deep assignment/system pages.

## Requirements

- Windows 10 version 2004 or newer, or Windows 11.
- .NET 8 SDK for building from source.
- BOSS GT-001 connected over USB.
- GT-001 MIDI receive channel set to channel 1.

The app uses the GT-001 USB MIDI ports:

- Main SysEx port: `GT-001 Ver1-1`
- Patch/program-change port, when present: `GT-001 Ver1-1 DAW CTRL`

## Safety Notes

The editor works against the GT-001 temporary patch buffer while you edit. Changes affect the current sound immediately, but they are not stored permanently until you use `WRITE`.

The `WRITE` button asks for a User patch destination and patch name. It overwrites that User patch on the device. Factory patches are treated as read-only.

Before using WRITE heavily, test with a patch slot you do not mind replacing.

## Build

Restore, build, and test with the normal .NET CLI:

```powershell
dotnet restore GT001.Editor.sln --configfile NuGet.Config
dotnet build GT001.Editor.sln --no-restore
dotnet test GT001.Editor.sln --no-build
```

Run the app from source:

```powershell
dotnet run --project src\GT001.Editor.App\GT001.Editor.App.csproj
```

Create a local publish folder:

```powershell
dotnet publish src\GT001.Editor.App\GT001.Editor.App.csproj -c Release -r win-x64 --self-contained false -o publish\GT001.Editor
```

## Basic Use

1. Connect the GT-001 over USB.
2. Start GT-001 Editor.
3. Open the connection menu in the top-right header.
4. Select the GT-001 input and output ports.
5. Click `Connect`.
6. Select a patch from the User or Factory tabs.
7. Edit parameters in the main panel.
8. Click `WRITE` to save the current temporary patch to a User patch number and name.

## Repository Notes

The `reference/` and `screenshots/` folders are ignored intentionally. Do not commit Roland/BOSS manuals, copied manual tables, screenshots of vendor manuals, or other reference material unless you have the right to redistribute it.

The source code contains independently modeled parameter metadata needed by the editor, but this repository should not include the vendor PDFs themselves.

## Third-Party Components

- [DryWetMIDI](https://github.com/melanchall/drywetmidi) for MIDI access.
- Microsoft Windows App SDK / WinUI for the desktop UI.

Third-party packages remain under their own licenses.

## Disclaimer

This is an independent, unofficial project. It is not affiliated with, endorsed by, or supported by Roland Corporation or BOSS. Product names and trademarks belong to their respective owners.

## License

MIT. See [LICENSE](LICENSE).
