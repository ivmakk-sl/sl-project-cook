# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-09-21

### Added

- Tooltip on a dish card: the tier of the dish, the cooking XP of the pot, and the trade value at each quality level. It also shows the recovery bonus and the Perfect morale bonus of the character's talents, which apply when the character eats the dish.
- Tier mark on the food badge of each ingredient in the cooking window: gold with a triangle up for High-end, grey with a triangle down for Low-grade.
- Trade value line in the ingredient tooltip.
- `IgnoreCookingTalents` setting in a `Debug` section, a test aid that makes the cooking talents have no effect. Off by default.

### Changed

- Quality chances include the cooking talents of the character ("Practice Makes Perfect", "Tag Master", "Mold Master").
- A game update that renames a part of the cooking window now turns off only the feature that needs it, and the log names the missing part.

### Fixed

- An error while the preview draws is written to the log, not lost.

## [1.0.0] - 2026-09-20

### Added

- Dish preview in the "THIS POT" panel of the cooking window: each dish card shows one line for each quality level that can occur, with its chance, the dish stats (satiety, morale, stamina, health, life), and the portion count.
- Ingredient tooltips in the cooking window show the tier of the ingredient and its raw stats.
- English and Chinese: the added text follows the game language and uses the game's own words.
- `Verbose` setting that logs each preview and each cooked result, to compare the two in a bug report. Off by default.
