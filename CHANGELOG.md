# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Codex marks: a gold trophy at the top left of a dish card's icon in "THIS POT" marks a dish that is not in your Codex yet, also on new combinations and "???" cards. The food sort gets the choice Codex: the foods you have never tasted come first, each with a trophy, and nothing is dimmed. The Food Storage of the Rat Cage shows the trophy too. The cooking window has no choice Codex: there the trophy marks only the dishes. No trophy shows in guide mode and explore mode, where the game keeps the Codex locked. Turn this off with `CodexMarks`.
- The Recipe Book rows show the same trophy for a dish that is not in your Codex yet. The Recipe Book title shows the button "Codex A / B" (dishes in your Codex / all dishes of the Codex) in place of "Unlocked A / B": click it to show only the rows with a trophy, in the same order, and again to show all rows. The choice stays until you quit the game. Part of `CodexMarks`.
- Load in the Recipe Book also takes the ingredients from the other storage tabs of the cooking window (the Backpack, fridges, and cooking storages) when the open tab does not have them all. Each ingredient leaves its own tab; locked tabs give nothing. The Recipe Book counts all these tabs, so a row shows Load whichever tab is open. With the open tab enough, Load works as before. Turn this off with `LoadFromAllTabs`.

## [1.4.0] - 2026-10-23

### Added

- Storages tagged Cooking or Food also show as tabs in the Brewing Barrel, Rat Cage (each size), and Ration Press windows, after the fridge tabs. They lock with the fridge tabs of the window: until cooking level 2 in the Brewing Barrel and Ration Press windows, never in the Rat Cage window. Drag items from them as from a fridge tab, and in the Rat Cage window sort them with the Sort dropdown as the fridge tabs. Part of `CookingStorages`.
- Compact tabs: the storage tabs of the cooking, Brewing Barrel, Rat Cage, and Ration Press windows show the icon of each storage in place of its name, so more tabs fit in the row. The Backpack tab shows the word Inventory, as in the workbench window; a storage with no icon of its own, or with a stand-in icon of the game (a bed on the Medium Storage Rack, a shelf on the pre-installed Refrigerator), shows the icon of the same furniture model or name, or else the first word of its name. Point at a tab to see its name and, on a locked tab, the game's lock text. When you drag an item onto a tab in the cooking window, a drop just above or below the tab also counts, the tab's name shows above the tab row, and the item turns half transparent. Turn this off with `IconTabs`.

## [1.3.0] - 2026-10-17

### Added

- Cook by row from cooking level 3: each row of the ingredient grid is matched to recipes separately. Three Frozen Pork Chops in three rows give three Salt and Pepper Pork Chops, each with its own quality roll and cooking XP. A row can give more than one dish; a row with no matching recipe gives none. Seasonings still give their quality bonus to every dish and their tier to recipes that use ingredient types. "THIS POT" previews the dishes by row. With one row, cooking works as before; cooking time stays the same. Turn this off with `RowSplit` in the config file's `[Features]` section.
- Separate pieces: drop a piece of Wild Rabbit or another ingredient with more than one use on free cells in the ingredient grid to keep it separate. Each piece counts as an ingredient, so two pieces in two rows can make two dishes. Drop it onto an item of the same kind to merge them, if that item is not full. Quick moves and hot pot mode work as before. Turn this off with `SeparatePieces`.
- Uncookable items appear dimmed in the cooking window's tabs, including items that are not food, products such as Beef Slices, and ingredients that need cutting first. Fuel such as Scrap Paper stays bright at a stove that burns fuel. Turn this off with `DimUncookable`.
- Whole dish / Per portion switch after the "THIS POT" title. Portion counts ("xN") stay visible in both views. The choice lasts until you quit and resets to Whole dish at each game start. This is part of `DishPreview`.
- Food sort in the Rat Cage window, under Leave All and Leave by Type, for the Backpack and each fridge tab. Satiety shows what the cage gets from all remaining uses, highest first, without talent bonuses. Items the cage does not accept come last and are dimmed. The cage's Food Storage grid shows the selected numbers too. The sort choice is shared with the storage and cooking windows.
- Cooking tag icon above storage, in the tag's color beside the other tag icons.
- Store Chilled Food also fills fridges tagged Food or Cooking, then storage with either tag in the Cold Storage room while the chiller runs or the room is sealed. Priority: Chilled, tagged fridges, untagged fridges, then the Cold Storage room. Tags still decide which food each storage accepts; an ordinary cabinet outside the room does not qualify. The button also works while you stand in the room, using days left at the normal spoilage speed. Chilled still uses the longer days left in the cold room and may reject food that Food or Cooking accepts. Outside the room, the same food qualifies as before. Turn this off with `ColdSortFood`.

### Changed

- Dish cards for the same recipe each show previews for their own ingredients.
- A divider separates the mod's ingredient tooltip lines from the game's text. Colors and text sizes stay the same.
- Ingredient tooltips stay inside the window so their lines remain visible.
- Tooltip additions from this mod and other mods appear once each, in a consistent order.
- The food sort menu closes when you click outside it. The click still works and your sort choice stays selected.
- Default appears in white in the food sort menu. Gold marks only an active sort.
- If a game update uses the Cooking tag's ID for a game tag, the mod turns its Cooking tag off so the game's tag works normally. Other features keep working.

### Removed

- Support for Survival Log versions before 1.1.18902.

### Fixed

- Eat values include the base part of Satiety Bonus, Morale Bonus, Stamina Bonus, Fitness Bonus, and Life Bonus.
- Dimmed items that occupy several cells remain opaque, hiding the grid lines behind them when playing without Compact Inventory.
- The Backpack's Sort button appears before Store Chilled Food and Store Materials again.

## [1.2.0] - 2026-10-03

### Added

- Food sort for a fridge and a storage with the Cooking tag or the tag Food, in the storage window and in the container tabs of the cooking window (the Backpack too, in both windows): Default, Satiety, Morale, Stamina, Life, Trade value, or Expiration Date, each with an icon. The items show in the order of the choice, with a number at the top left of each cell (stats in green or red, the trade value in gold). While a sort is on, the dropdown shows the choice in gold, and the workbench of the cooking window shows the numbers too. Expired food comes first, with the days until it spoils in red. The sort does not move the items in the storage, and one choice applies to both windows until you quit the game.
- Settings to turn off each feature (`DishPreview`, `FoodSort`, `CookingStorages` in the `[Features]` section of the config file), for example when another mod conflicts with it. All are on by default.
- Cooking tag in the Status row of the tag rule. A storage with this tag or with the game's tag Food shows as a container tab in the cooking window, so you can cook with its items. The tag does not change what the robot puts in or takes out, or how fast food spoils.

### Changed

- The stats on the dish cards are what your character gets from the whole dish: they include the eat talents (for example Nourishing Richness, Carnivore, Efficient Diet, Optimist, and Perfect Morale) and the Life factor of the difficulty. They leave out temporary buffs. Fitness is no longer on the card, because eating does not change it.
- The trade values in the dish card tooltip and the ingredient tooltip include the appraisal talents of your character.
- The dish card tooltip no longer has the separate recovery and Perfect Morale lines; these bonuses are in the card stats now.

### Fixed

- The dish card lines no longer overflow the card with all four stats and portions. They show the chance in the color of its quality level, without the quality name, and use a smaller font only when they are too wide.

## [1.1.1] - 2026-09-27

The Juicer and the Coffee Machine open the same cooking window as a stove, so their drinks get the same preview lines, dish card tooltip, ingredient tooltips, and tier badges. This worked in 1.1.0 too; the README and the Nexus page now say so.

### Changed

- Much less work for the game's UI browser: the mod sends its page script to the browser once, and a change of the ingredients sends only a short call. The ingredient tooltips go again only after a change of the game language.
- The preview lines of a dish card have their own style, so the game's one-line cut of the card hint does not apply to them.

### Fixed

- Portion count of a dish with a satiety just above the portion threshold. A satiety of 40.3 with a threshold of 40 now shows 2 portions, as the game gives, not 1.

## [1.1.0] - 2026-09-21

### Added

- Tooltip on a dish card: the tier of the dish, the cooking XP for that dish, and its base trade value at each quality level. It also shows the recovery bonus and the Perfect morale bonus of the character's talents, which apply when the character eats the dish. These bonuses are shown separately from the dish stats.
- Tier mark on the food badge of each ingredient in the cooking window: gold with a triangle up for High-end, grey with a triangle down for Low-grade.
- Base trade value line in the ingredient tooltip.

### Changed

- Quality chances include the cooking talents of the character ("Practice Makes Perfect", "Tag Master", "Mold Master").
- If a game update changes part of the cooking window, unaffected preview features can continue to work. The log names any missing part.

### Fixed

- Preview errors are now written to the log to help diagnose problems.

## [1.0.0] - 2026-09-20

### Added

- Dish preview in the "THIS POT" panel of the cooking window: each dish card shows one line for each quality level that can occur, with its chance, the dish stats (satiety, morale, stamina, health, life), and the portion count.
- Ingredient tooltips in the cooking window show the tier of the ingredient and its raw stats.
- English and Chinese: the added text follows the game language and uses the game's own words.
