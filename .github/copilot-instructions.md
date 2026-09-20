# Copilot code-review instructions

This repo is a BepInEx 6 (IL2CPP) Harmony mod for *Survival Log*. Plugins derive from `BasePlugin` and call the game through Il2CppInterop proxy assemblies. Review with these traps in mind; a general C# review misses most of them.

## IL2CPP Harmony traps

- **Getter/setter patches often never fire.** il2cpp inlines trivial accessors, so a `MethodType.Getter`/`.Setter` patch silently does nothing. Flag a new getter patch used as the only mechanism. The reliable change is mutating the backing field at a load hook.
- **No `is`/`as` across the interop boundary.** Flag `is`, `as`, or a direct cast on a game type. The correct form is `x.TryCast<T>()` then a null check.
- **No `foreach` over game collections.** The interop enumerator lacks the pattern. Expect `GetEnumerator()` / `MoveNext()` / `Current`, or a count plus an indexer.
- **Never read an `Il2CppSystem.ValueTuple<...>` result of a game method,** direct or as a list element. The interop layer reads the fields wrongly and gives garbage with no error (seen with `Reducer_Web_Cooking.PreMatchRecipes` and `CookingFormula.CalcSplit`). Expect a method that returns a class, a dictionary, or an `Il2CppStructArray`, or the value calculated in the mod.
- **Guard game lookups.** Singletons, config lookups, and the web view return null often. Flag an unchecked dereference inside a patch.
- **A patch must not break the game.** Each patch body sits in a try/catch that logs the error, so the cooking window falls back to the game's own display.

## Web UI

- The game UI is web pages in one web view: a root page hosts each panel as an iframe. To change what a panel shows, change the state that the reducer writes, in a Harmony Postfix. New DOM comes from a script run in the root page through `ExecuteJavaScript`, which reaches the panel through `iframe.contentWindow`.
- A reopened panel is a new iframe. Flag a page hook that installs once and assumes it stays.
- A page hook wraps the game's function and calls the original first. Flag a hook that replaces the original, or page code with no try/catch around the added DOM work.
- Text that goes into a script string must have no quote or backslash, or must be escaped.

## Structure and tests

- **Pure logic is separated and tested.** Logic that does not need the running game (chance math, line text, JSON edits) lives in its own file with no BepInEx or Il2Cpp reference, unit-tested under `tests/`. Flag new pure logic in `Plugin.cs`, and new pure logic with no test.
- `netstandard2.1` has no JSON library and the game's Newtonsoft stub cannot be used, so `FlatJson.cs` is on purpose. Do not suggest `System.Text.Json`.
- **Patches** prefer a postfix, and tie the `Harmony` instance to the plugin GUID.
- **Display only.** The mod must not change the cooking result, the recipe selection, or the save. Flag a patch that writes game state other than the displayed prediction list.

## Release and config hygiene

- **Verbose ships off.** The `Verbose` config binds with default `false`. Diagnostic tracing goes on `LogDebug` behind it; `LogInfo` stays quiet apart from the load line.
- **The plugin GUID never changes.** It is `com.ivmakk.survivallog.projectcook`, the BepInEx identity and the config file name. Flag any edit to it.
- **The version is in two places that must agree:** `<Version>` in the csproj and the `BepInPlugin` attribute.
- **No committed build output.** Flag `bin/`, `obj/`, `dist/`, or a game DLL in the diff. Game `<Reference>` entries keep `<Private>false</Private>`.
- **Changelog matches the change.** A player-visible change adds an `[Unreleased]` entry to `CHANGELOG.md` in player-facing wording. An internal-only refactor gets none.
