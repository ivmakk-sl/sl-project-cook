# Configuration

Project Cook creates `BepInEx\config\com.ivmakk.survivallog.projectcook.cfg` in the game folder the first time you start the game with the mod installed. Close the game, open this file in a text editor, change the settings you want, and save it. Changes take effect the next time you start the game.

The section names below match the sections in the config file.

## Features

Each feature is on by default. Set its value to `false` to turn it off and keep the other features.

| Setting | Mod default | Values | What it does |
| --- | --- | --- | --- |
| `DishPreview` | `true` | `true` / `false` | Shows quality chances, eat values, and portion counts on dish cards. Adds dish card tooltips, the portion switch after the "THIS POT" title, ingredient tooltip lines, and ingredient tier badges. |
| `FoodSort` | `true` | `true` / `false` | Lets you sort food by stat, trade value, or expiration date in the storage window, the cooking window, and the Rat Cage window. Shows the selected value on items, including cooking workbench ingredients. |
| `RowSplit` | `true` | `true` / `false` | From cooking level 3, each row of the cooking station is its own group of ingredients, and the game matches the dishes of each row alone. With all ingredients in one row, the cook works as without the mod. |
| `SeparatePieces` | `true` | `true` / `false` | A piece of an ingredient with more than one use (for example Wild Rabbit 1/3) that you drop on a free cell of the cooking station stays its own item. A drop on an item of the same kind merges the piece into that item. |
| `DimUncookable` | `true` | `true` / `false` | Dims the items that the cooking station does not take in each tab of the cooking window: an item that is not food, a product such as Beef Slices, or an item that needs cutting first. Fuel items stay bright at a stove that burns fuel. |
| `CookingStorages` | `true` | `true` / `false` | Lets you use ingredients directly from home storages tagged Cooking or Food in the cooking window, from cooking level 2. Adds the Cooking tag to storage settings. |

With `SeparatePieces` off, each piece that joins the cooking station merges into an item of the same kind, as without the mod.

With `RowSplit` off, each cook works as without the mod: the game puts all ingredients in one pool. A cook that runs while you change the setting ends with the recipes that it started with, and the game then gives the ingredients to these recipes with its own rule.

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
```

The sort choice and the choice of the portion switch are selected in the window, not in this file. The sort choice is shared between the storage window, the cooking window, and the Rat Cage window and resets to Default when you restart the game. The portion switch resets to Whole dish when you restart the game.

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
