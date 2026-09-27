# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
