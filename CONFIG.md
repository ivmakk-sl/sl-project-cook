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
