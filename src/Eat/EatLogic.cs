using System;
using System.Collections.Generic;

namespace ProjectCook
{
    // Game-free logic of the eat values: what the character gets from a whole item (all portions together) with its
    // fixed talents and the difficulty factor, in the order of the game's eat path (AE_ItemAddBase5Attr.Run, then
    // AttributeComponent.CostAttrValue for each value). This file must not use any game or BepInEx type, because the
    // unit tests compile it alone.
    public static class EatLogic
    {
        // Index of a stat in the game's arrays: satiety, morale, stamina, fitness (the game's Health), life.
        public const int Satiety = 0, Morale = 1, Stamina = 2, Fitness = 3, Life = 4;

        // CostAttrValue clamps the sum of an Add ratio (per mille) to this range.
        private const int MinPermille = -500, MaxPermille = 2000;
        // The range of the food-type factor.
        private const float MinFoodType = -1f, MaxFoodType = 2f;

        public sealed class Dish
        {
            // The five values of the whole item: the instance values of a cooked dish, else the config values.
            public float[] Values;
            // A cooked dish has its own values (InstanceEffectEnd); only then the nourish factor applies.
            public bool HasInstanceValues;
            // Category 1 and SubCategory 1: the condition of Comfort Eater.
            public bool IsStaple;
            // The config id is a PerfectItemID: each portion gets the Perfect Morale talents.
            public bool IsPerfect;
            // The food type of the food-type talents (1 to 11).
            public int SubCategory;
            public int Portions = 1;
        }

        // One owned talent level and what it does to the eat path. A value that the talent does not have is 0 or null.
        public sealed class Talent
        {
            public string Name;
            // Buff/AE_CookDishNourish: times (1 + Nourish) for an item with instance values.
            public float Nourish;
            // Buff/AE_CookEatPerfectMorale: a Morale gain for each eaten portion of a Perfect dish.
            public float PerfectMorale;
            // The Add ratios (BuffSatietyAdd ...) per mille, by stat index.
            public int[] AddPermille;
            // Buff/AE_FoodTypeBonus: the Satiety and the Morale ratio, by SubCategory.
            public float[] SatietyFood, MoraleFood;
            // The condition talents: Efficient Diet (any food) and Comfort Eater (staple food), per mille of the
            // Satiety Add ratio, for a gain.
            public int EfficientPermille, ComfortPermille;
        }

        public sealed class Factors
        {
            public List<Talent> Talents = new List<Talent>();
            // The base value of each Add ratio attribute of the character, per mille, by stat index. Null is 0.
            public int[] BasePermille;
            // DifficultyTools.GetVitalityItemAddDelta: times (1 + LifeDelta) for a Life gain, not below 0.
            public float LifeDelta;
            // The name of the difficulty, for the log of the eat factors.
            public string DifficultyName;
        }

        public static float[] EatValues(Dish dish, Factors factors)
        {
            var result = new float[5];
            var nourish = 0f;
            var perfectMorale = 0f;
            foreach (var t in factors.Talents)
            {
                nourish += t.Nourish;
                perfectMorale += t.PerfectMorale;
            }
            for (var stat = 0; stat < 5; stat++)
            {
                if (stat == Fitness) continue;
                var value = Get(dish.Values, stat);
                if (dish.HasInstanceValues) value *= 1f + nourish;
                result[stat] = Gain(stat, value, dish, factors);
            }
            if (dish.IsPerfect && perfectMorale != 0f)
                result[Morale] += Math.Max(1, dish.Portions) * Gain(Morale, perfectMorale, dish, factors);
            return result;
        }

        // CostAttrValue for a gain from an item: the food-type factor (Satiety, Morale) or the difficulty factor
        // (Life), then the Add ratio. A loss goes through the Del ratio, which no talent of the mod's list changes.
        private static float Gain(int stat, float value, Dish dish, Factors factors)
        {
            if (value <= 0f) return value;
            var foodType = 0f;
            var permille = Get(factors.BasePermille, stat);
            foreach (var t in factors.Talents)
            {
                if (stat == Satiety) foodType += Get(t.SatietyFood, dish.SubCategory);
                if (stat == Morale) foodType += Get(t.MoraleFood, dish.SubCategory);
                permille += Get(t.AddPermille, stat);
                if (stat == Satiety) permille += t.EfficientPermille + (dish.IsStaple ? t.ComfortPermille : 0);
            }
            if (stat == Satiety || stat == Morale) value *= 1f + Math.Min(MaxFoodType, Math.Max(MinFoodType, foodType));
            if (stat == Life) value = Math.Max(0f, (1f + factors.LifeDelta) * value);
            return value * (1f + Math.Min(MaxPermille, Math.Max(MinPermille, permille)) / 1000f);
        }

        private static float Get(float[] values, int i) => values != null && i >= 0 && i < values.Length ? values[i] : 0f;

        private static int Get(int[] values, int i) => values != null && i >= 0 && i < values.Length ? values[i] : 0;
    }
}
