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
- Text that goes into a script string must have no quote or backslash, or must be escaped (`PageJson.Str`).
- **Send once.** The page script (`src/Web/page/*.ts`, TypeScript built by Vite into one file that the plugin embeds) goes to the root page only when the root page has no script. A prediction refresh sends only `window.__projectCook.apply()`, and `setData(data)` goes only when the ingredient data changes (a language switch) or with the script. The game-free `src/Web/PushSchedule.cs` decides each send and is the only retry. Flag a send of the script or the ingredient data on each refresh, and a retry loop (`setTimeout`) in the page script.
- **Styles live in CSS files.** `src/Web/tokens.css` holds the `--pc-` tokens and `src/Web/page.css` the rules; stylelint checks that a rule uses a token, not a raw color. The script sets classes (`projectcook-*`). Flag a raw color or an inline style in the page script, except the column count `--pc-cols` and the `display` of the game's `#recipeTooltip`.

## Structure and tests

- **Feature folders.** `src/Plugin.cs` holds only the plugin: the config, the patch list, and `Load`. Each feature has its folder (`src/Preview/`, `src/ResultLog/`, `src/Debug/`), and `src/Web/` holds the page script, its CSS, and the C# that sends it. One patch class for each target method, named `<Feature>On<Target>`, attached on its own in the patch list of `Load`.
- **Pure logic is separated and tested.** Logic that does not need the running game (chance math, line text, JSON edits, the page JSON, the send schedule) lives in its own file with no BepInEx or Il2Cpp reference, unit-tested under `tests/`. Flag new pure logic in a patch class, and new pure logic with no test. The page script has Vitest tests against the game's `Cooking.html` under `tests/page/`.
- **The per-frame tick is guarded.** `PageTick` (a Postfix of `WebUILayer.OnUpdate`, which also runs while the cooking window pauses the world) runs inside one try/catch, and each distinct warning logs once. Flag work in it that runs on each frame while no send is pending.
- `netstandard2.1` has no JSON library and the game's Newtonsoft stub cannot be used, so `FlatJson.cs` is on purpose. Do not suggest `System.Text.Json`.
- **Patches** prefer a postfix, and tie the `Harmony` instance to the plugin GUID.
- **Display only.** The mod must not change the cooking result, the recipe selection, or the save. Flag a patch that writes game state other than the displayed prediction list.

## Release and config hygiene

- **Verbose ships off.** The `Verbose` config binds with default `false`. Diagnostic tracing goes on `LogDebug` behind it; `LogInfo` stays quiet apart from the load line.
- **The plugin GUID never changes.** It is `com.ivmakk.survivallog.projectcook`, the BepInEx identity and the config file name. Flag any edit to it.
- **The version is in two places that must agree:** `<Version>` in the csproj and the `BepInPlugin` attribute.
- **No committed build output.** Flag `bin/`, `obj/`, `dist/`, `node_modules/`, or a game DLL in the diff. The build runs Vite, so it needs `npm ci` at the mod root; a change of the npm packages commits `package-lock.json`. Game `<Reference>` entries keep `<Private>false</Private>`.
- **Changelog matches the change.** A player-visible change adds an `[Unreleased]` entry to `CHANGELOG.md` in player-facing wording. An internal-only refactor gets none.
