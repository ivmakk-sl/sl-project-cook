using System;
using System.Runtime.CompilerServices;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.Codex;

namespace ProjectCook
{
    // One read of the game's codex for one build (the dish cards of one prediction, or the items of one sort build).
    // The codex of all runs is in BattleLogicWorld._CodexManager. IsEntryUnlocked does no grouping, so a dish asks for
    // the representative recipe of its dish, as TryUnlock stores it. When the manager or a member is missing (a game
    // update), the reader gives no marks and logs one warning.
    internal sealed class CodexReader
    {
        private static bool warned;

        private readonly CodexManager manager;
        internal readonly bool Locked;
        private bool broken;

        private CodexReader(CodexManager manager, bool locked) { this.manager = manager; Locked = locked; }

        // Null when the codex cannot be read: no marks.
        internal static CodexReader Take()
        {
            try { return Read(); }
            catch (Exception e) { Warn(e); return null; }
        }

        // Members of the game are used only in methods without a try, so a missing member fails at the call and the
        // caller catches it.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CodexReader Read()
        {
            var manager = BaseSingleton<BattleLogicWorld>.Instance?._CodexManager;
            if (manager == null) throw new InvalidOperationException("BattleLogicWorld._CodexManager is null");
            return new CodexReader(manager, manager.IsLocked);
        }

        // True: the dish card of this prediction RecipeId shows the codex mark.
        internal bool DishMark(int recipeId)
        {
            if (Locked || broken) return false;
            try { return CodexLogic.ShowMark(false, true, DishUnlocked(CodexLogic.DishSource(recipeId))); }
            catch (Exception e) { broken = true; Warn(e); return false; }
        }

        // True: the item is a food of the codex (CodexManager.IsInCodex). Read here, so a missing InCodex field fails
        // only the codex mark, not the sort build.
        internal bool IsFoodEntry(Config_Item cfg)
        {
            if (broken) return false;
            try { return ReadFoodEntry(cfg); }
            catch (Exception e) { broken = true; Warn(e); return false; }
        }

        // True: the food entry shows the codex mark (the player has never tasted it).
        internal bool FoodMark(int configId)
        {
            if (Locked || broken) return false;
            try { return CodexLogic.ShowMark(false, true, FoodUnlocked(configId)); }
            catch (Exception e) { broken = true; Warn(e); return false; }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ReadFoodEntry(Config_Item cfg) => CodexLogic.IsFoodEntry(cfg.Category, cfg.InCodex);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool DishUnlocked(int recipeId) =>
            manager.IsEntryUnlocked(CodexCategory.Dish, DishCodexGrouping.Representative(recipeId));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool FoodUnlocked(int configId) => manager.IsEntryUnlocked(CodexCategory.Food, configId);

        private static void Warn(Exception e)
        {
            if (warned) return;
            warned = true;
            Plugin.Log.LogWarning($"Project Cook cannot read the game's codex, so no codex mark shows: {e.Message}");
        }
    }
}
