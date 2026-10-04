# Project Cook

A mod for the Steam game *Survival Log* that shows what a dish will be before you cook it. In the cooking window, each dish card of the "THIS POT" panel gets one line for each quality level that can occur: the chance of that level (in the color of the level; point at it for the name), what your character gets from the whole dish at that level (satiety, morale, stamina, life), and the portion count. So you can see if one more seasoning or a better ingredient is worth it, before the ingredients are gone.

The stats on the card are eat values: the dish stats with the eat talents of your character (for example Nourishing Richness, Carnivore, Efficient Diet, Optimist, and the Morale of Perfect Morale) and the Life factor of the difficulty. They leave out temporary buffs (also talents that last a limited time, such as Leisure Time), random talent extras, being tired of a food, and the stat maximum. Fitness is not on the card, because eating does not change it.

The Juicer and the Coffee Machine open the same cooking window, so their drinks get the same preview lines, dish card tooltip, ingredient tooltips, and tier badges as the dishes of a stove.

The switch after the "THIS POT" title changes the stats of the cards between the whole dish and one portion. With "Per portion", each stat is the stat of the whole dish divided by the portion count of that line, and the portion count stays on the line. Whole dish is the choice at each game start, and the choice stays until you quit the game.

Point at a dish card to see more: the tier of the dish, the cooking XP for that dish, and its trade value at each quality level.

Ingredient tooltips in the cooking window also show the tier of the ingredient (High-end, Mid-tier, Low-grade), its trade value, and its raw stats. The trade values include the appraisal talents of your character (Shrewd Appraisal, Bargaining), and not the demand of a trader. The food badge of an ingredient shows its tier at a glance: a gold badge with a triangle that points up is High-end, a grey badge with a triangle that points down is Low-grade, and the game's green badge is Mid-tier or no tier.

The added text follows the game language, English and Chinese. The quality, tier, and stat names are the game's own words, so they are equal to the rest of the window.

In each tab of the cooking window, the items that the cooking station does not take are dimmed: an item that is not food, a product such as Beef Slices, or an item that needs cutting first. At a stove that burns fuel, the fuel items (for example Scrap Paper) are not dimmed, because the fuel queue takes them. The dim does not change the item, its place, or a drag.

Apart from the row split and the separate pieces (see below), Project Cook does not change the cooking result, the recipes, or what you get from food. The stats and the portion count come from the game's own formula. The chances use the same inputs as the game's quality roll: cooking level, cook furniture, fresh or expired ingredients, seasonings, exact recipe, and the cooking talents of the character. Hot pot mode has no preview.

## Row split

From cooking level 3 the game can cook more than one dish at once, but it puts all ingredients in one pool and picks the recipe that uses the most ingredients first. Project Cook lets you choose the dishes: each row of the cooking station (the ingredient grid of the cooking window) is its own group of ingredients, and the game matches the dishes of each row alone, with its own rule.

- Three Pork Chops in three rows give three Salt and Pepper Pork Chops, each with its own quality roll and cooking XP. In one row they give one mixed dish, as without the mod.
- A row can still give more than one dish, the same dishes as a cook with only that row.
- A meat and a vegetable in the first row and another meat and a vegetable in the second row give two dishes, each with the tier and the stats of its own row.
- A seasoning in a row can be an ingredient of a recipe of that row. Each seasoning on the cooking station still gives its quality bonus to every dish, and its tier to every dish of ingredient types, as without the mod. So a row with only seasonings works.
- A row whose items match no recipe gives no dish.
- An item that covers two rows belongs to the row of its top cell.

The "THIS POT" panel shows the dishes of the rows, from the top row down, each with its own preview lines. With all ingredients in one row, or below cooking level 3, the cook works as without the mod. The cook time stays the game's.

## Separate pieces

An ingredient with more than one use, such as Wild Rabbit (1/3, 2/3, 3/3), joins the cooking station one piece at a time. Without the mod each piece merges into the item of the same kind that is already on the cooking station, and a recipe counts one item as one ingredient, whatever its pieces. With Project Cook:

- A piece that you drop on a free cell stays its own item there. A recipe counts it as its own ingredient, so two pieces in two rows give two dishes with the row split.
- A piece that you drop on an item of the same kind merges into that item, as without the mod. When that item cannot take it (it is full, or the game does not merge the two, for example for a different freshness), the piece goes to a free cell as its own item.
- A quick move to the cooking station, and hot pot mode, work as without the mod.

## Food sort

A fridge and a cooking storage get a Sort dropdown with a sort icon on the line of the Auto Organize button, in the storage window and in the container tabs of the cooking window, and so does the Backpack, in the cooking window and on the Backpack side of the storage window while a fridge or a cooking storage is open: Default, Satiety, Morale, Stamina, Life, Trade value, and Expiration Date, each with its icon. A choice other than Default shows the items in its order, with the number of each item at the top left of its cell, and the dropdown shows the icon and the name of the choice in gold, so you see that the grid is sorted:

- Satiety, Morale, Stamina, Life: the eat value of one use, highest first. A positive number is green, a negative number is red.
- Trade value: the value of the whole stack with your appraisal talents, highest first, in gold.
- Expiration Date: the days until the item expires, fewest first, in white. An expired item comes first and shows in red the days until it spoils (a rotten item shows "Rotten").
- Items with no number for the choice come last and are dimmed. Equal items stand together.

In the cooking window, the items on the workbench show the number of the choice too, at their places, so you can compare them with the open tab.

The Rat Cage window has the dropdown too, under the Leave All and Leave by Type buttons, for the Backpack and each fridge tab. Here Satiety shows the satiety that the cage gets from the item, not the eat value: the satiety of the dish itself for a cooked dish or a ration, else the raw satiety, times the uses left, with no talent bonus. So the same item can show a bigger Satiety number here than in the cooking window. Items that the cage does not take come last and are dimmed. The Food Storage grid of the cage shows the number of the choice at each item.

The sort only changes what the window shows, not the places of the items. One choice applies to all these windows until you quit the game. While a sort is on, a drag inside the sorted grid does nothing; a drag to or from another grid works as usual, and an item that you put in goes to a free place. Select Default to move items by hand.

## Cooking tag

The tag rule of a storage ("What goes in here") gets the tag Cooking in its Status row. A storage with this tag shows as a container tab in the cooking window, after the fridges, so you can cook with its items (from cooking level 2, as the fridge tabs). It is for food that keeps long and that you still cook with. A storage with the game's tag Food also shows as a tab, with no Cooking tag needed. The tag does not change what the robot puts in or takes out: with other tags the storage works as with those tags alone, and with only the Cooking tag it works as with the tag Food. It does not change how fast food spoils.

Nexus page: https://www.nexusmods.com/survivallog/mods/13

## How the ingredient tier works

**Quality and tier are separate.** Quality is the cooking result: Perfect, Good, Average, or Failed. Tier is High-end, Mid-tier, or Low-grade and matters for recipes that use ingredient types, such as Meat + Vegetables. A High-end dish can still turn out Failed, and a Low-grade dish can turn out Perfect. Project Cook shows the dish tier and the chance of each quality.

- The tier matters only for a recipe that asks for ingredient types (for example Meat + Vegetables). A recipe that asks for specific items has fixed stats and ignores the tier.
- The dish gets the tier of the highest-tier ingredient used by the recipe. One High-end ingredient is enough; more of them add nothing to the tier. Cooking talents can also raise the dish tier.
- The tier multiplies the ingredient totals for satiety, health, and life: High-end x1.7, Mid-tier x1.4, Low-grade x1.1. Quality applies another multiplier, and satiety also gets a flat bonus per ingredient. The tier also selects the dish (for example Eight-Treasure Stew, Stew, or Simple Stew), and with it the base trade value.
- The multiplier works on the sum of the ingredient stats. A High-end ingredient with weak stats (for example Enoki Mushrooms) can raise the tier and the trade value, and still lower the stats. The dish card shows the final numbers, so compare there.
- The tier of an ingredient comes from its price. Staple food, snacks, and drinks have no tier.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ProjectCook.dll`

## Settings

The config file `BepInEx\config\com.ivmakk.survivallog.projectcook.cfg` (written at the first game start with the mod) has a switch for each feature in its `[Features]` section. Each is `true` by default. Set one to `false` to turn that feature off, for example when another mod conflicts with it, then restart the game.

- `DishPreview` - the preview lines and the tooltip of the dish cards, the portion switch, and the added ingredient lines and tier marks.
- `FoodSort` - the food sort in the storage window, the cooking window, and the Rat Cage window.
- `RowSplit` - each row of the cooking station as its own group of ingredients, from cooking level 3.
- `SeparatePieces` - a piece that you drop on a free cell of the cooking station stays its own item.
- `DimUncookable` - the dim of the items that the cooking station does not take, in the tabs of the cooking window.
- `CookingStorages` - the Cooking tag and the tabs of Cooking and Food storages in the cooking window. Off works like an uninstall of this part: the game drops the Cooking tag from each storage on the next save (see [Uninstall](#uninstall)), and turning it on again does not bring the tag back.

See [CONFIG.md](CONFIG.md) for all settings, defaults, and instructions for editing the file.

## Uninstall

Delete `ProjectCook.dll` from the `BepInEx\plugins` folder. The game then drops the Cooking tag from each storage, and keeps the items and the other tags. A storage with only the Cooking tag loses its rule, and the robot no longer puts items in it. Before you uninstall, give such a storage a tag of the game (for example Food).

## Troubleshooting

The mod was verified on the Steam build `25366138` of the game with BepInEx `6.0.0-be.788`. A game update can change the cooking window. If the preview lines do not show, look in `BepInEx\LogOutput.log` for the `Project Cook loaded.` line and for a warning or an error from Project Cook.

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
