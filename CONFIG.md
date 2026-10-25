# Configuration

Project Cook creates `BepInEx\config\com.ivmakk.survivallog.projectcook.cfg` in the game folder the first time you start the game with the mod installed. Close the game, open this file in a text editor, change the settings you want, and save it. Changes take effect the next time you start the game.

The section names below match the sections in the config file.

## Features

Each feature is on by default. Set its value to `false` to turn it off and keep the other features.

| Setting | Mod default | Values | What it does |
| --- | --- | --- | --- |
| `DishPreview` | `true` | `true` / `false` | Shows quality chances, eat values, and portion counts on dish cards. Adds dish card tooltips, the portion switch after the "THIS POT" title, ingredient tooltip lines, and ingredient tier badges. |
| `FoodSort` | `true` | `true` / `false` | Lets you sort food by stat, trade value, or expiration date in the storage window, the cooking window, and the Rat Cage window. Shows the selected value on items, including cooking workbench ingredients. |
| `RowSplit` | `true` | `true` / `false` | From cooking level 3, matches recipes for each row of the ingredient grid separately. Row splitting has no effect when all ingredients are in one row. |
| `SeparatePieces` | `true` | `true` / `false` | Keeps a piece of an ingredient with more than one use (for example Wild Rabbit 1/3) separate when you drop it on free cells in the ingredient grid. Drop it onto an item of the same kind to merge them, if that item is not full. Quick moves and hot pot mode work as usual. |
| `DimUncookable` | `true` | `true` / `false` | Dims items you cannot cook in each tab of the cooking window: items that are not food, products such as Beef Slices, or ingredients that need cutting first. Fuel stays bright at a stove that burns fuel. |
| `CookingStorages` | `true` | `true` / `false` | Lets you use ingredients directly from home storages tagged Cooking or Food in the cooking, Brewing Barrel, Rat Cage, and Ration Press windows, from cooking level 2 (in the Rat Cage window at any level). Adds the Cooking tag to storage settings. |
| `ColdSortFood` | `true` | `true` / `false` | The Store Chilled Food button of the backpack also fills a fridge tagged Food or Cooking, and last a storage tagged Food or Cooking in the Cold Storage room while the room keeps food cold. A storage tagged Chilled still comes first, and a fridge with no tag comes before the Cold Storage room. In the Cold Storage room, the button counts the food in the backpack by its days at the normal speed. Off: the button works as without the mod. |
| `IconTabs` | `true` | `true` / `false` | Shows the icon of each storage in place of its name on the storage tabs of the cooking, Brewing Barrel, Rat Cage, and Ration Press windows, so more tabs fit in the row. Point at a tab to see its name. Off: the tabs show their names as without the mod. |
| `CodexMarks` | `true` | `true` / `false` | Shows a gold trophy on each dish card and each Recipe Book row whose dish is not in your Codex yet, puts the Codex button ("Codex A / B") in the Recipe Book title in place of "Unlocked A / B" (click it to show only the rows with a trophy), and adds the choice Codex to the food sort: the foods you have never tasted come first, each with a trophy. A dish that a cooking station cooks now (also paused) shows no trophy in the Recipe Book and leaves the Codex list until the cook ends. Off: no trophy, no Codex button, and no choice Codex. No trophy and no Codex button show in guide mode and explore mode, where the game keeps the Codex locked. |
| `LoadFromAllTabs` | `true` | `true` / `false` | Load in the Recipe Book also takes the ingredients from the other storage tabs of the cooking window (the Backpack, each fridge, and each cooking storage that is not locked) when the open tab does not have them all, and the Recipe Book counts all these tabs. With a full home, a refresh of the Recipe Book can take a few milliseconds more. Off: Load takes only the open tab, as without the mod. |
| `UnknownRecipesWithIngredients` | `true` | `true` / `false` | With the Codex button of the Recipe Book on, the list also shows the recipes you have not learned yet, whose dish is not in your Codex and has no row in the Recipe Book, and whose ingredients the cooking station and the open tab hold (with `LoadFromAllTabs`, all the storage tabs): each with a grey "?", the trophy, the dish name, its ingredients, and Load. The first cook teaches the recipe. Only at a cooking station where you know at least one dish, and not for a recipe that needs a cooking level or an event first. Needs `CodexMarks`. A refresh of the Recipe Book can take about a millisecond more. Off: no row of a recipe that you have not learned, unless `UnknownRecipesWithoutIngredients` is on. |
| `UnknownRecipesWithoutIngredients` | `false` | `true` / `false` | With the Codex button of the Recipe Book on, the list also shows the recipes you have not learned yet whose ingredients you do not have, after the rows with Load, with the same rules and look as `UnknownRecipesWithIngredients` but no Load. With both on: every recipe that the station can cook, a full spoiler. Works also with `UnknownRecipesWithIngredients` off. Needs `CodexMarks`. |

With `SeparatePieces` off, each piece that joins the cooking station merges into an item of the same kind, as without the mod.

With `RowSplit` off, the game matches recipes against all the ingredients together. If you restart with this setting off while a cook is in progress, it finishes with its saved recipes. The game assigns ingredients to those recipes using its usual rules, so the dishes may differ from the row previews.

**Before turning off `CookingStorages`:** give any storage whose only tag is Cooking a game tag such as Food. Otherwise, it loses its tag rule and the robot no longer puts items there.

With this feature off, the extra storage tabs disappear, but the game's fridge tabs remain. The game removes Cooking tags when it loads your save and keeps that removal on the next save. Your items and other tags stay. Turning the feature on again does not restore removed Cooking tags.

For example, to keep the previews and cooking storage tabs but turn off the food sort:

```ini
[Features]
DishPreview = true
FoodSort = false
CookingStorages = true
RowSplit = true
SeparatePieces = true
DimUncookable = true
ColdSortFood = true
IconTabs = true
CodexMarks = true
LoadFromAllTabs = true
UnknownRecipesWithIngredients = true
UnknownRecipesWithoutIngredients = false
```

The sort choice and the choice of the portion switch are selected in the window, not in this file. The sort choice is shared between the storage window, the cooking window, and the Rat Cage window and resets to Default when you restart the game. The portion switch resets to Whole dish when you restart the game. The Codex button of the Recipe Book is off (all rows) after each restart.

## General

| Setting | Mod default | Values | What it does |
| --- | --- | --- | --- |
| `Verbose` | `false` | `true` / `false` | Writes detailed debug entries to the BepInEx log for troubleshooting. Keep off during normal play. |

To include these entries in `BepInEx\LogOutput.log`, also add `Debug` to `LogLevels` under `[Logging.Disk]` in `BepInEx\config\BepInEx.cfg`. Keep the other log levels in that list.

## Debug

| Setting | Mod default | Values | What it does |
| --- | --- | --- | --- |
| `IgnoreCookingTalents` | `false` | `true` / `false` | Disables cooking talent effects in both the preview and actual cooking, including cooking XP bonuses. For tests only. Keep off during normal play. |

`IgnoreCookingTalents` changes the dishes you cook while it is on. It does not remove your character's talents, and turning it off restores their effects after a restart.
