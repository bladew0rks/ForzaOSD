<p align="center">
  <img src="FOSD_logo.png" alt="ForzaOSD" width="420">
</p>

# ForzaOSD

ForzaOSD is an external HUD overlay for Forza Horizon 6. It listens to Forza's
telemetry stream and draws a transparent, click-through overlay over
the game. It does not inject into the game, hook it, or read its memory.

The HUDs are Lua profiles, so each one owns its layout, assets and settings. The
app ships several recreations of racing-game HUDs. The VFD radio module can run
alongside any speedometer and display audio
spectrum and music metadata.

## Getting started

1. Extract the release folder and run `forzaosd.exe`.
2. In Forza, enable **Data Out**, use IP `127.0.0.1`, and set the port to `5300`.
3. Press **Shift+Esc** to choose a HUD, position it, and save your settings.

## Building

Windows 10/11 and the .NET 10 SDK are required. The repository-local SDK is used
automatically when present.

```powershell
.\.dotnet\dotnet build ForzaOSD.slnx -c Release
.\.dotnet\dotnet test src\ForzaOSD.Tests\ForzaOSD.Tests.csproj -c Release
.\build.ps1
```

`build.ps1` creates a self-contained Windows x64 ZIP in `dist`; users do not need
to install .NET. See [Lua.md](Lua.md) to make a HUD profile and [credits.txt](credits.txt)
for the original HUD projects represented here.

## License

ForzaOSD is GPL-3.0-or-later. HUD assets remain the property of their respective creators.
