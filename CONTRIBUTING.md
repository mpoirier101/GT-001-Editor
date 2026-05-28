# Contributing

Thanks for taking a look at GT-001 Editor.

## Development Setup

Use the standard .NET 8 SDK on Windows:

```powershell
dotnet restore GT001.Editor.sln --configfile NuGet.Config
dotnet build GT001.Editor.sln --no-restore
dotnet test GT001.Editor.sln --no-build
```

The app requires real GT-001 hardware for end-to-end MIDI testing.

## Reference Material

Do not commit vendor manuals, copied manual tables, screenshots of manuals, or other files that are not redistributable. The local `reference/` and `screenshots/` folders are ignored for this reason.

When adding parameter coverage, prefer small, source-code metadata updates and focused tests rather than committing extracted documentation.

## Pull Requests

Please describe:

- The device/app behavior changed.
- Any GT-001 hardware testing performed.
- Any patches or memory slots used during WRITE testing.

Keep changes focused when possible. MIDI write behavior should be treated carefully and tested against expendable User patch slots first.
