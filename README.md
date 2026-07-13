<p align="center">
  <img src="FOSD_logo.png" alt="ForzaOSD" width="420">
</p>

# ForzaOSD

ForzaOSD is an external HUD overlay for Forza Horizon 6. It reads Forza's telemetry stream over UDP and draws a transparent, click-through overlay on top of the game. No injection, no hooking, no memory reading.

Every HUD is a Lua profile. Each one owns its own layout, assets, and settings, so nothing bleeds into the next. The app ships with a handful of recreations of well-known racing HUDs, and there's a separate VFD radio module that runs alongside any speedometer and shows the audio spectrum plus whatever's playing. Profiles hot-reload while you work on them: save the file, glance at the overlay, keep going.

## What it does

ForzaOSD only listens to Forza's telemetry UDP packets. It never touches the game process itself.

Layout, assets, fonts, and settings all live in the profile script, so tweaking a gauge doesn't mean recompiling anything. Edits get picked up automatically, and there's a diagnostics view if a reload goes wrong, so one bad save doesn't wreck the overlay you're relying on mid-race. A `hud` profile is the main speedometer; you can stack any number of `module` profiles on top of it, like the VFD radio. And the release build bundles its own runtime, so whoever's using it doesn't need .NET installed on their machine.

## Getting started

1. Extract the release folder and run `forzaosd.exe`.
2. In Forza, enable **Data Out**, set the IP to `127.0.0.1`, and the port to `5300`.
3. Press **Shift+Esc** to pick a HUD, position it, and save your settings.

## Building

You'll need Windows 10/11 and the .NET 10 SDK. If a repository-local SDK is present, it's used automatically.

```powershell
.\.dotnet\dotnet build ForzaOSD.slnx -c Release
.\.dotnet\dotnet test src\ForzaOSD.Tests\ForzaOSD.Tests.csproj -c Release
.\build.ps1
```

`build.ps1` produces a self-contained Windows x64 ZIP in `dist`. Nobody downloading it needs .NET installed.

See [Lua.md](Lua.md) for the full HUD profile API, and [credits.txt](credits.txt) for the original HUD projects this one draws from.

## License

ForzaOSD is licensed under GPL-3.0. HUD assets remain the property of their respective creators.