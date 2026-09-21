# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
