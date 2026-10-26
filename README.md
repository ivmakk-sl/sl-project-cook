# Project Cook

The cooking window tells you which dish you get, but not how good it will be. See whether better ingredients or more seasoning are worth using, and arrange ingredients by row to choose which dishes you cook together.

## Main features

- **Dish previews.** Each dish card in "THIS POT" shows the chance, eat values, and portion count for each possible quality. Drinks from the Juicer and Coffee Machine get previews too.
- **Portion switch.** Switch the dish cards between the stats of the whole dish and of one portion.
- **Cooking tooltips.** Hover over dish cards for tier, cooking XP, and trade values. Ingredient tooltips show raw stats and trade values, with tier badges on ingredients.
- **Cook by row.** From cooking level 3, each row of the ingredient grid is matched to recipes separately. Three Frozen Pork Chops in three rows give three Salt and Pepper Pork Chops, each with its own quality roll and cooking XP.
- **Separate pieces.** Drop a piece of Wild Rabbit or another ingredient with more than one use on free cells in the ingredient grid to keep it separate. Drop it onto an item of the same kind to merge them, if that item is not full. Quick moves and hot pot mode work as usual.
- **Dim uncookable items.** Items you cannot cook, such as Beef Slices or ingredients that need cutting first, appear dimmed in the cooking window's tabs. Fuel stays bright at a stove that burns fuel.
- **Food sort.** Find food by Satiety, Morale, Stamina, Life, Trade value, or Expiration Date, with numbers on each item. Also in the Rat Cage window.
- **Codex marks.** A gold trophy on a dish card or a Recipe Book row marks a dish that is not in your Codex yet. The Codex button in the Recipe Book title shows only those recipes. A dish that a cooking station cooks now leaves that list until its cook ends, so you do not cook the same new dish twice. The Codex choice of the food sort puts the foods you have never tasted first, each with a trophy.
- **Unknown recipes in the Codex list.** With the Codex button on, the Recipe Book also lists the recipes you have not learned yet whose ingredients you have, each with a grey "?" and Load. Turn it off with `UnknownRecipesWithIngredients`. To also see the ones whose ingredients you do not have (a full spoiler), turn on `UnknownRecipesWithoutIngredients`.
- **Load from all tabs.** Load in the Recipe Book also takes the ingredients from the other storage tabs of the cooking window, when the open tab does not have them all. The Recipe Book counts all these tabs, so a row shows Load whichever tab is open. Locked tabs give nothing.
- **Cook from home storages.** Use ingredients directly from cabinets and other home storages in the cooking window, including the Juicer and Coffee Machine, and in the Brewing Barrel, Rat Cage, and Ration Press windows. Add the Cooking tag to a storage, or use its existing Food tag. Available from cooking level 2, as the fridge tabs; in the Rat Cage window at any level.
- **Compact tabs.** Storage tabs show icons instead of names, so more fit in the row: at the Cooking Station (Electric Oven, Juicer, and other cooking furniture), the Brewing Barrel, the Rat Cage, and the Ration Press. Point at a tab to see its name. At the Cooking Station, dropping an item on a tab is easier: a drop just above or below it counts, and its name shows while you drag.
- **Store Chilled Food.** The snowflake button in your Backpack also fills fridges tagged Food or Cooking, then storage with either tag in the Cold Storage room while the room keeps food cold. Storage tagged Chilled still comes first, followed by tagged fridges, untagged fridges, and finally the Cold Storage room.

## Preview details

Each preview line shows the chance in its quality's color (hover for the quality name), the whole dish's Satiety, Morale, Stamina, and Life, and its portion count.

The stats on the card are eat values: the dish stats with the eat talents of your character (for example Nourishing Richness, Carnivore, Efficient Diet, Optimist, and the Morale of Perfect Morale) and the Life factor of the difficulty. They leave out temporary buffs (also talents that last a limited time, such as Leisure Time), random talent extras, being tired of a food, and the stat maximum. Fitness is not on the card, because eating does not change it.

The Juicer and the Coffee Machine open the same cooking window, so their drinks get the same preview lines, dish card tooltip, ingredient tooltips, and tier badges as the dishes of a stove.

The switch after the "THIS POT" title changes the stats of the cards between the whole dish and one portion. With "Per portion", each stat is the stat of the whole dish divided by the portion count of that line, and the portion count stays on the line. Whole dish is the choice at each game start, and the choice stays until you quit the game.

Point at a dish card to see more: the tier of the dish, the cooking XP for that dish, and its trade value at each quality level.

Ingredient tooltips in the cooking window also show the tier of the ingredient (High-end, Mid-tier, Low-grade), its trade value, and its raw stats. The trade values include the appraisal talents of your character (Shrewd Appraisal, Bargaining), and not the demand of a trader. The food badge of an ingredient shows its tier at a glance: a gold badge with a triangle that points up is High-end, a grey badge with a triangle that points down is Low-grade, and the game's green badge is Mid-tier or no tier.

The added text follows the game language, English and Chinese. Quality, tier, and stat names match the game's own text.

Uncookable items appear dimmed in each tab. This includes items that are not food, products such as Beef Slices, and ingredients that need cutting first. Fuel such as Scrap Paper stays bright at a stove that burns fuel. Dimming does not change items, their places, or dragging.

Apart from the row split and the separate pieces (see below), Project Cook does not change the cooking result, the recipes, or what you get from food. The stats and the portion count come from the game's own formula. The chances use the same inputs as the game's quality roll: cooking level, cook furniture, fresh or expired ingredients, seasonings, exact recipe, and the cooking talents of the character. Hot pot mode has no preview.

## Row split

From cooking level 3 the game can cook more than one dish at once, but it puts all ingredients in one pool and picks the recipe that uses the most ingredients first. Project Cook lets you choose the dishes: each row of the cooking station (the ingredient grid of the cooking window) is its own group of ingredients, and the game matches the dishes of each row alone, with its own rule.

- Three Frozen Pork Chops in three rows give three Salt and Pepper Pork Chops, each with its own quality roll and cooking XP. In one row they give one mixed dish, as without the mod.
- A row can still give more than one dish, the same dishes as a cook with only that row.
- A meat and a vegetable in the first row and another meat and a vegetable in the second row give two dishes, each with the tier and the stats of its own row.
- A seasoning in a row can be an ingredient of a recipe of that row. Each seasoning on the cooking station still gives its quality bonus to every dish, and its tier to every dish of ingredient types, as without the mod. So a row with only seasonings works.
- A row whose items match no recipe gives no dish.
- An item that covers two rows belongs to the row of its top cell.

The "THIS POT" panel shows the dishes of the rows, from the top row down, each with its own preview lines. Row splitting has no effect with all ingredients in one row or below cooking level 3. Cooking time stays the same.

## Separate pieces

An ingredient with more than one use, such as Wild Rabbit (1/3, 2/3, 3/3), joins the cooking station one piece at a time. Without the mod each piece merges into the item of the same kind that is already on the cooking station, and a recipe counts one item as one ingredient, whatever its pieces. With Project Cook:

- A piece that you drop on a free cell stays its own item there. A recipe counts it as its own ingredient, so two pieces in two rows give two dishes with the row split.
- A piece that you drop on an item of the same kind merges into that item, as without the mod. A drop on a full item does nothing. When the game does not merge the piece into an item that is not full, the piece goes to a free cell as its own item.
- A quick move to the cooking station, and hot pot mode, work as without the mod.

## Food sort

A fridge and a cooking storage get a Sort dropdown with a sort icon on the line of the Auto Organize button, in the storage window and in the container tabs of the cooking window, and so does the Backpack, in the cooking window and on the Backpack side of the storage window while a fridge or a cooking storage is open: Default, Satiety, Morale, Stamina, Life, Trade value, and Expiration Date, each with its icon. A choice other than Default shows the items in its order, with the number of each item at the top left of its cell, and the dropdown shows the icon and the name of the choice in gold, so you see that the grid is sorted:

- Satiety, Morale, Stamina, Life: the eat value of one use, highest first. A positive number is green, a negative number is red.
- Trade value: the value of the whole stack with your appraisal talents, highest first, in gold.
- Expiration Date: the days until the item expires, fewest first, in white. An expired item comes first and shows in red the days until it spoils (a rotten item shows "Rotten").
- Items with no number for the choice come last and are dimmed. Equal items stand together.

In the cooking window, the items on the workbench show the number of the choice too, at their places, so you can compare them with the open tab.

The Rat Cage window has the dropdown too, under the Leave All and Leave by Type buttons, for the Backpack and each fridge and cooking storage tab. Here Satiety shows the satiety that the cage gets from the item, not the eat value: the satiety of the dish itself for a cooked dish or a ration, else the raw satiety, times the uses left, with no talent bonus. So the same item can show a bigger Satiety number here than in the cooking window. Items that the cage does not take come last and are dimmed. The Food Storage grid of the cage shows the number of the choice at each item.

The sort only changes what the window shows, not the places of the items. One choice applies to all these windows until you quit the game. While a sort is on, a drag inside the sorted grid does nothing; a drag to or from another grid works as usual, and an item that you put in goes to a free place. Select Default to move items by hand.

## Codex marks

See what is still missing from your Codex:

- Each dish card in "THIS POT" whose dish is not in the Codex yet shows a gold trophy at the top left of its icon. This includes new combinations and "???" cards. A dish that is in the Codex from any of its recipes shows no trophy.
- Each Recipe Book row of such a dish shows the same trophy at its icon, also a craft-locked row. A row of several recipes of one dish shows one trophy.
- The title of the Recipe Book shows the button "Codex A / B" in place of "Unlocked A / B": A is the dishes in your Codex, B is all dishes of the Codex. Click it to show only the rows with a trophy, in the same order; click it again to show all rows. Load and the other clicks of a row work as usual. The choice stays until you quit the game. Point at the button to see how many of the missing dishes are in your Recipe Book.
- While a cooking station of your home cooks a dish (also when the cook pauses for lack of power or fuel), its Recipe Book rows show no trophy, the Codex button does not show them, and its tooltip does not count the dish. So when you cook at several stations, you do not pick the same new dish twice. When the cook ends, the dish is in the Codex. When you stop the cook, the trophy comes back. "Codex A / B" and the dish cards change only when the cook ends.
- The food sort of the storage window and the Rat Cage window gets the choice Codex, with a trophy icon. It puts the foods that you have never tasted first, each with a trophy, and the other items after them in their real order. Nothing is dimmed. The items in the Food Storage of the Rat Cage show the trophy at their places. The cooking window has no choice Codex, because cooking an ingredient does not add it to the Codex: there the trophy marks only the dishes.

The trophies follow the Codex of all your runs. After you cook a dish or eat a food for the first time, its trophy is gone at the next refresh of the window. In guide mode and explore mode the game keeps the Codex locked, so no trophy shows.

### Unknown recipes in the Codex list

The Recipe Book lists only the dishes of which you know a recipe. The game teaches a recipe only when you cook it for the first time or reach its cooking level, so a new dish usually means a guess of its ingredients. With the Codex button on, the Recipe Book also lists the recipes that you have not learned yet:

- Only a recipe whose dish is not in your Codex, whose dish has no row in the Recipe Book, and whose ingredients the cooking station and the storage tabs hold (with Load from all tabs, all its tabs; without it, the open tab).
- Each row has the game's grey "?" in place of the dish picture, the trophy, the dish name, its ingredients, Load, and the game's tooltip. Load fills the cooking station as for any recipe, and the first cook teaches you the recipe.
- The list shows the rows with Ready first, then the rows with Load, then the others. In each part, your known recipes come first.
- Only at a cooking station where you know at least one dish. A recipe that needs a cooking level or an event first does not show.
- `UnknownRecipesWithIngredients` (on by default) shows the unknown recipes whose ingredients you have. `UnknownRecipesWithoutIngredients` (off by default) shows the ones whose ingredients you do not have, after the rows with Load. With both on, the list shows every recipe that the station can cook. Each switch works alone.

The recipes, the ingredients, and the cook come from the game: the costs, times, and results stay the same as by hand.

Creative Cook (Medium) and Creative Cook (Premium) raise the tier of a dish from ingredient types (for example Meat + Vegetables), so the card shows the higher-tier dish and its trophy. The lower-tier dishes of these recipes (for example Simple Stew) need a run without the talent to enter the Codex.

## Load from all tabs

The game's Load in the Recipe Book takes the ingredients only from the cooking station and the open storage tab. With Project Cook:

- When those two do not have all the ingredients, Load also takes them from the other storage tabs of the cooking window: the Backpack, each fridge, and each cooking storage, from left to right as the tab row shows them. Each ingredient leaves the tab it came from.
- When the open tab has them all, Load works as without the mod and takes nothing from another tab.
- A locked tab (a fridge or a cooking storage below cooking level 2) gives nothing.
- The Recipe Book counts the ingredients of all these tabs, so a row shows Load, and keeps its place in the list, whichever tab is open.
- The game still picks the items in each tab, also the ingredient tier of a recipe of ingredient types, and still moves the unused items on the cooking station to the open tab.

## Cooking tag

The tag rule of a storage ("What goes in here") gets the tag Cooking in its Status row. A storage with this tag shows as a tab after the fridges in the cooking window, the Brewing Barrel window, the Rat Cage window, and the Ration Press window, so you can use its items there. The tab unlocks with the fridge tabs of the window: from cooking level 2, and at any level in the Rat Cage window. It is for food that keeps long and that you still cook with. A storage with the game's tag Food also shows as a tab, with no Cooking tag needed. The tag does not change what the robot puts in or takes out: with other tags the storage works as with those tags alone, and with only the Cooking tag it works as with the tag Food. It does not change how fast food spoils.

## Compact tabs

The storage tabs of the cooking, Brewing Barrel, Rat Cage, and Ration Press windows show icons in place of names:

- The Backpack tab shows the word Inventory, as the workbench window does.
- A fridge, a cooking storage, or a tool cabinet tab shows the icon of its furniture. A furniture with no icon of its own (most furniture found in the world), or with a stand-in icon of the game (the pre-installed Refrigerator shows a shelf in the game data, the Medium Storage Rack a bed), shows the icon of the same furniture model or of a furniture with the same name, or else the first word of its name.

Two storages of the same kind show the same icon, as they show the same name without the mod. Point at a tab to see its name, and for a locked tab the game's lock text. Clicking a tab and the locked look work as usual.

In the cooking window you can drop an item on a tab, as without the mod, and the small tabs make that easier: a drop up to 16 px above or below a tab counts as a drop on it, the name of the tab under the item shows above the tab row, and the item turns half transparent so you see the tab through it.

Turn this off with `IconTabs`.

## Store Chilled Food

Store Chilled Food, the snowflake button in your Backpack in the storage window, moves food with more than 0 and less than 10 days left before expiration. Without the mod, it uses storage tagged Chilled and untagged fridges. Project Cook adds:

- a fridge (Fridge, Refrigerator, Double-Door Fridge, Deluxe Fridge, Freezer, Large-Capacity Freezer, Display Freezer) tagged Food or Cooking;
- a storage in the Cold Storage room of the warehouse home with the tag Food or Cooking, while the room keeps food cold: the Cold Storage Chiller runs, or the room is sealed.

The button fills storage in this order: Chilled, fridges tagged Food or Cooking, untagged fridges, then tagged storage in the Cold Storage room. Food moves to a later group only when earlier destinations that accept it have no room. Tags still decide which items a storage accepts, as they do for the robot: Meat with Cooking takes only meat. Food accepts any food; the game keeps this tag only when no other food tag is selected. A cabinet tagged Food outside the Cold Storage room stays out because food spoils there at the normal speed.

In the Cold Storage room, food in your Backpack spoils more slowly, which can make the game hide the button. Project Cook uses the food's days left at the normal spoilage speed, so you can store it while standing in the room too. The game's Chilled tag still uses the longer days left in the cold room, so it may reject food that the Food or Cooking tag accepts.

Outside the Cold Storage room the button still takes the same items (only food that needs cold). The robot rules, the tag rules, and how fast food spoils stay the same. Turn it off with `ColdSortFood`.

Nexus page: https://www.nexusmods.com/survivallog/mods/13

## How the ingredient tier works

**Quality and tier are separate.** Quality is the cooking result: Perfect, Good, Average, or Failed. Tier is High-end, Mid-tier, or Low-grade and matters for recipes that use ingredient types, such as Meat + Vegetables. A High-end dish can still turn out Failed, and a Low-grade dish can turn out Perfect. Project Cook shows the dish tier and the chance of each quality.

- The tier matters only for a recipe that asks for ingredient types (for example Meat + Vegetables). A recipe that asks for specific items has fixed stats and ignores the tier.
- The dish gets the tier of the highest-tier ingredient used by the recipe. One High-end ingredient is enough; more of them add nothing to the tier. Cooking talents can also raise the dish tier.
- The tier multiplies the ingredient totals for Satiety, Fitness, and Life: High-end x1.7, Mid-tier x1.4, Low-grade x1.1. Quality applies another multiplier, and Satiety also gets a flat bonus per ingredient. The tier also selects the dish (for example Eight-Treasure Stew, Stew, or Simple Stew), and with it the base trade value.
- The multiplier works on the sum of the ingredient stats. A High-end ingredient with weak stats (for example Enoki Mushrooms) can raise the tier and the trade value, and still lower the stats. The dish card shows the final numbers, so compare there.
- The tier of an ingredient comes from its price. Staple food, snacks, and drinks have no tier.

## Requirements

- Survival Log 1.1.18902 or later.
- [BepInEx for Survival Log](https://www.nexusmods.com/survivallog/mods/12). Install it first.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ProjectCook.dll`

## Settings

The config file `BepInEx\config\com.ivmakk.survivallog.projectcook.cfg` (written at the first game start with the mod) has a switch for each feature in its `[Features]` section. Each is `true` by default. Set one to `false` to turn that feature off, for example when another mod conflicts with it. The change applies while you play: at once in an in-game config manager, or after an edit of the file at the next save load or when you open Settings. `CookingStorages` applies at the next game start.

- `DishPreview` - the preview lines and the tooltip of the dish cards, the portion switch, and the added ingredient lines and tier marks.
- `FoodSort` - the food sort in the storage window, the cooking window, and the Rat Cage window.
- `RowSplit` - each row of the cooking station as its own group of ingredients, from cooking level 3.
- `SeparatePieces` - a piece that you drop on a free cell of the cooking station stays its own item.
- `DimUncookable` - dims items you cannot cook in the cooking window's tabs.
- `CookingStorages` - the Cooking tag and the tabs of Cooking and Food storages in the cooking, Brewing Barrel, Rat Cage, and Ration Press windows. Off works like an uninstall of this part: the game drops the Cooking tag from each storage on the next save (see [Uninstall](#uninstall)), and turning it on again does not bring the tag back.
- `ColdSortFood` - the fridges with Food or Cooking and the Cold Storage room storages as targets of Store Chilled Food.
- `IconTabs` - the storage icons in place of the names on the tabs of the cooking, Brewing Barrel, Rat Cage, and Ration Press windows.
- `CodexMarks` - the trophy on the dish cards and the Recipe Book rows whose dish is not in the Codex, the Codex button of the Recipe Book, and the Codex choice of the food sort.
- `LoadFromAllTabs` - Load in the Recipe Book also takes the ingredients from the other storage tabs of the cooking window.
- `UnknownRecipesWithIngredients` - the Codex list of the Recipe Book also shows the recipes you have not learned yet whose ingredients you have.
- `UnknownRecipesWithoutIngredients` (off by default) - the Codex list of the Recipe Book also shows the recipes you have not learned yet whose ingredients you do not have.

See [CONFIG.md](CONFIG.md) for all settings, defaults, and instructions for editing the file.

## Uninstall

Delete `ProjectCook.dll` from the `BepInEx\plugins` folder. The game then drops the Cooking tag from each storage, and keeps the items and the other tags. A storage with only the Cooking tag loses its rule, and the robot no longer puts items in it. Before you uninstall, give such a storage a tag of the game (for example Food).

If a cook is in progress when you remove the mod or turn off `RowSplit`, it finishes with its saved recipes. The game assigns ingredients to those recipes using its usual rules, so the dishes may differ from the row previews.

## Troubleshooting

Project Cook 1.4.0 was verified on Survival Log `1.1.19131` (Steam build `26076180`) with BepInEx `6.0.0-be.788`. A game update can change the cooking window. If the preview lines do not show, look in `BepInEx\LogOutput.log` for the `Project Cook loaded.` line and for a warning or an error from Project Cook.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies are game-derived and are not part of this repo. The .NET 8 SDK is required.

The build also builds the page script (TypeScript and CSS in `src/Web/`) with Vite, so Node is required too. The mod root has a `mise.toml` for Node, and the npm packages install once after a clone:

```
mise trust
npm ci
dotnet build src/ProjectCook.csproj -c Release
```

`Directory.Build.props` sets `GameDir` to the default Steam install path. If the game is in another place, override it without an edit of the file: set a `GameDir` environment variable, or pass `-p:GameDir=...` on the build. The output DLL is at `src\bin\Release\ProjectCook.dll`.

The preview math, the rows of the row split, the merge target of a piece, the text of the lines, the JSON for the page script, and the send schedule are game-free code (`src/Preview/PreviewLogic.cs`, `src/RowSplit/RowSplitLogic.cs`, `src/Pieces/PiecesLogic.cs`, `src/Web/PageJson.cs`, `src/Web/PushSchedule.cs`, and the JSON library in `src/Shared/json/`) with unit tests. `src/Shared/json/` is a library copy of the JSON library of the modding workspace (`JsonText`, `FlatJson`), at the version that its `VERSION` file names; it is not edited in this repo. The tests do not need the game:

```
dotnet test tests/ProjectCook.Tests
```

The page script has its own tests, which run the built script against the real `Cooking.html` of the installed game (Vitest with jsdom). Run them after a game update. They need the game install, and `SL_GAME_DIR` overrides the default Steam path. The other page checks run at the mod root too:

```
npm test            # builds the page script, then runs the page tests
npm run lint        # stylelint on the CSS files
npm run typecheck   # TypeScript check of the page script and its tests
npm run dev         # the game's cooking window in a browser with the page script and fake data
```

## Package

Add `-p:Package=true` to a Release build to also write the ready-to-install zip at `dist\ProjectCook-<version>.zip`, laid out as `BepInEx\plugins\ProjectCook.dll` so a user extracts it at the game root. A plain build skips this step.

```
dotnet build src/ProjectCook.csproj -c Release -p:Package=true
```

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk. See [LICENSE](LICENSE).

You may reuse and modify this mod, but you must keep it open under the same license and give credit. Do not reupload it without credit.

The trophy icon (`src/Web/page/icons/codex.svg`) is from [Lucide](https://lucide.dev), ISC License, Copyright (c) Lucide Contributors.
