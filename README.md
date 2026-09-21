# Project Cook

A mod for the Steam game *Survival Log* that shows what a dish will be before you cook it. In the cooking window, each dish card of the "THIS POT" panel gets one line for each quality level that can occur: the chance of that level, the stats of the dish at that level (satiety, morale, stamina, health, life), and the portion count. So you can see if one more seasoning or a better ingredient is worth it, before the ingredients are gone.

Point at a dish card to see more: the tier of the dish, the cooking XP that the pot gives, and the trade value of the dish at each quality level. If the character has a talent that raises the recovery from its own dishes, or one that gives morale for a Perfect dish, the tooltip shows that bonus too. These two bonuses apply when the character eats the dish, so they are not part of the stats on the card.

Ingredient tooltips in the cooking window also show the tier of the ingredient (High-end, Mid-tier, Low-grade), its trade value, and its raw stats, the same values as in the item window of the game. The food badge of an ingredient shows its tier at a glance: a gold badge with a triangle that points up is High-end, a grey badge with a triangle that points down is Low-grade, and the game's green badge is Mid-tier or no tier.

The added text follows the game language, English and Chinese. The quality, tier, and stat names are the game's own words, so they are equal to the rest of the window.

The mod only changes what the cooking window displays. It does not change the cooking result, the recipes, or the save. The stats and the portion count come from the game's own formula, so they are equal to what the cooked dish gives. The chances use the same inputs as the game's quality roll: cooking level, cook furniture, fresh or rotten ingredients, seasonings, exact recipe, and the cooking talents of the character. Hot pot mode has no preview.

Nexus page: https://www.nexusmods.com/survivallog/mods/13

## How the ingredient tier works

The game does not explain the tier, so here is the short version:

- The tier matters only for a recipe that asks for ingredient types (for example Meat + Vegetables). A recipe that asks for specific items has fixed stats and ignores the tier.
- The dish gets the tier of its best ingredient, not the average. One High-end ingredient is enough, more of them add nothing to the tier.
- The tier multiplies satiety, health, and life of the dish: High-end x1.7, Mid-tier x1.4, Low-grade x1.1. It also selects the dish (for example Eight-Treasure Stew, Stew, or Simple Stew), and with it the trade value.
- The multiplier works on the sum of the ingredient stats. A High-end ingredient with weak stats (for example Enoki Mushrooms) can raise the tier and the trade value, and still lower the stats. The dish card shows the final numbers, so compare there.
- The tier of an ingredient comes from its price. Staple food, snacks, and drinks have no tier.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ProjectCook.dll`

## Uninstall

Delete `ProjectCook.dll` from the `BepInEx\plugins` folder.

## Troubleshooting

The mod was verified on the Steam build `25366138` of the game with BepInEx `6.0.0-be.788`. A game update can change the cooking window. If the preview lines do not show, look in `BepInEx\LogOutput.log` for the `Project Cook loaded.` line and for a warning or an error from Project Cook.

If a preview differs from the cooked dish, set `Verbose = true` under `[General]` in `BepInEx\config\com.ivmakk.survivallog.projectcook.cfg` (the file appears after the first start with the mod), and add `Debug` to `LogLevels` under `[Logging.Disk]` in `BepInEx\config\BepInEx.cfg`. Then cook the dish again. The log then holds one entry for each preview and one for each cooked result. Attach the log to the bug report. Keep `Verbose` off in normal play.

The config file also has `IgnoreCookingTalents` under `[Debug]`. It is a test aid: when it is `true`, the cooking talents of the character have no effect in the game and in the preview, and the log has a warning at each start. It changes real cooking results, so keep it `false` in normal play.

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

The page script `src/page.js` has its own test, which runs it against the real `Cooking.html` of the installed game (Node with jsdom). Run it after a game update. It needs the game install, and `SL_GAME_DIR` overrides the default Steam path:

```
cd tests/page
npm install
npm test
```

## Package

Add `-p:Package=true` to a Release build to also write the ready-to-install zip at `dist\ProjectCook-<version>.zip`, laid out as `BepInEx\plugins\ProjectCook.dll` so a user extracts it at the game root. A plain build skips this step.

```
dotnet build src/ProjectCook.csproj -c Release -p:Package=true
```

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk. See [LICENSE](LICENSE).

You may reuse and modify this mod, but you must keep it open under the same license and give credit. Do not reupload it without credit.
