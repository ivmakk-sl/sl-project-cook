# Project Cook

A mod for the Steam game *Survival Log* that shows what a dish will be before you cook it. In the cooking window, each dish card of the "THIS POT" panel gets one line for each quality level that can occur: the chance of that level, the stats of the dish at that level (satiety, morale, stamina, health, life), and the portion count. So you can see if one more seasoning or a better ingredient is worth it, before the ingredients are gone.

Ingredient tooltips in the cooking window also show the tier of the ingredient (High-end, Mid-tier, Low-grade) and its raw stats, the same values as in the item window of the game.

The added text follows the game language, English and Chinese. The quality, tier, and stat names are the game's own words, so they are equal to the rest of the window.

The mod only changes what the cooking window displays. It does not change the cooking result, the recipes, or the save. The stats and the portion count come from the game's own formula, so they are equal to what the cooked dish gives. The chances use the same inputs as the game's quality roll (cooking level, cook furniture, fresh or rotten ingredients, seasonings, exact recipe), but they do not include character talents that change the quality roll. Hot pot mode has no preview.

Nexus page: not published yet.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ProjectCook.dll`

## Uninstall

Delete `ProjectCook.dll` from the `BepInEx\plugins` folder.

## Troubleshooting

The mod was verified on the Steam build `25366138` of the game with BepInEx `6.0.0-be.788`. A game update can change the cooking window. If the preview lines do not show, look in `BepInEx\LogOutput.log` for the `Project Cook loaded.` line and for a warning or an error from Project Cook.

If a preview differs from the cooked dish, set `Verbose = true` under `[General]` in `BepInEx\config\com.ivmakk.survivallog.projectcook.cfg` (the file appears after the first start with the mod), and add `Debug` to `LogLevels` under `[Logging.Disk]` in `BepInEx\config\BepInEx.cfg`. Then cook the dish again. The log then holds one entry for each preview and one for each cooked result. Attach the log to the bug report. Keep `Verbose` off in normal play.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies are game-derived and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/ProjectCook.csproj -c Release
```

`Directory.Build.props` sets `GameDir` to the default Steam install path. If the game is in another place, override it without an edit of the file: set a `GameDir` environment variable, or pass `-p:GameDir=...` on the build. The output DLL is at `src\bin\Release\ProjectCook.dll`.

The preview math and the text of the lines are game-free code (`src/PreviewLogic.cs`, `src/FlatJson.cs`) with unit tests. The tests do not need the game:

```
dotnet test tests/ProjectCook.Tests
```

## Package

Add `-p:Package=true` to a Release build to also write the ready-to-install zip at `dist\ProjectCook-<version>.zip`, laid out as `BepInEx\plugins\ProjectCook.dll` so a user extracts it at the game root. A plain build skips this step.

```
dotnet build src/ProjectCook.csproj -c Release -p:Package=true
```

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk. See [LICENSE](LICENSE).

You may reuse and modify this mod, but you must keep it open under the same license and give credit. Do not reupload it without credit.
