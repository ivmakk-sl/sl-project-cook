using System;
using System.Collections.Generic;
using System.Globalization;
using SlShared.I18n;

namespace ProjectCook
{
    // Game-free logic of the numbers of the food sort: for each item of a fridge or a cooking storage, the eat value
    // of one use for each stat, the trade value of the stack, and the days left. The page sorts by them, so a change
    // of the choice needs no send. This file must not use any game or BepInEx type, because the unit tests compile
    // it alone.
    public static class SortLogic
    {
        // Index of a stat number: the four stats that have an eat value (no Fitness).
        public const int Sat = 0, Mor = 1, Sta = 2, Life = 3;

        private static readonly int[] EatIndex = { EatLogic.Satiety, EatLogic.Morale, EatLogic.Stamina, EatLogic.Life };

        public readonly struct DaysLeft
        {
            public readonly bool IsExpired, IsRotten, HasNumber;
            // The days as the game counts them (a fridge makes the time pass slower): the days left, or for an expired
            // item the days until it spoils (the "will spoil in" of the game). Only with HasNumber.
            public readonly float Days;

            private DaysLeft(bool expired, bool rotten, bool hasNumber, float days) { IsExpired = expired; IsRotten = rotten; HasNumber = hasNumber; Days = days; }

            // Expired, and the item has no time until it spoils (its rot threshold is 1 or less).
            public static DaysLeft Expired => new DaysLeft(true, false, false, 0f);
            public static DaysLeft ExpiredSpoils(float days) => new DaysLeft(true, false, true, days);
            public static DaysLeft Rotten => new DaysLeft(true, true, false, 0f);
            // An item that does not spoil, or an item that the game gives no shelf life.
            public static DaysLeft NoSpoil => new DaysLeft(false, false, false, 0f);
            public static DaysLeft Spoils(float days) => new DaysLeft(false, false, true, days);
        }

        // The days of an item as the shelf life line of the storage window gives them
        // (Reducer_Web_BackpackUI.GetShelfLifeText). life is Config_Item.Life in days, rotThreshold IT_Rot_Threshold,
        // the times are in game hours. After the expiry the item spoils when its used life reaches rotThreshold times
        // its life; the time scale of the storage slows that.
        public static DaysLeft DaysLeftOf(int life, float rotThreshold, int startTime, int timeLeft, int originalTimeLeft,
            float timeScale, float presetShelfScale, float now, bool compostFrozen)
        {
            if (life < 1 || compostFrozen) return DaysLeft.NoSpoil;
            float scale = presetShelfScale > 0f ? presetShelfScale : 1f;
            float hoursLeft = (float)(timeLeft + startTime) - now;
            float days = hoursLeft * scale / 24f;
            if (days > 0f) return DaysLeft.Spoils(days);
            if (rotThreshold <= 1f) return DaysLeft.Expired;
            float total = originalTimeLeft >= 1 ? originalTimeLeft : life * 24f;
            float used = total <= 0f ? 0f : (total - hoursLeft * timeScale) / total;
            if (used >= rotThreshold) return DaysLeft.Rotten;
            float hours = (rotThreshold - used) * total;
            if (timeScale > 0f) hours /= timeScale;
            return DaysLeft.ExpiredSpoils(hours * scale / 24f);
        }

        // The sort key of Days left, lowest first: Rotten, then Expired, then the expired items by the days until they
        // spoil, then the days left; null for an item that does not spoil.
        public static float? DaysKey(DaysLeft d)
        {
            if (d.IsRotten) return -100001f;
            if (d.IsExpired) return d.HasNumber ? -100000f + Math.Min(d.Days, 99999f) : -100000f;
            return d.HasNumber ? d.Days : (float?)null;
        }

        // The numbers of one item. Null is "no number": the cell gets no badge, and the item is dimmed and last.
        public sealed class Numbers
        {
            // The config id of the item: a tie of the numbers groups the same items.
            public int ConfigId;
            // By Sat, Mor, Sta, Life.
            public int?[] Stats;
            public int? Trade;
            public DaysLeft Days;
            // The cage satiety (CageSatiety): the Rat Cage window shows it for the choice Satiety.
            public int? Cage;
        }

        public sealed class Words
        {
            // Default, Satiety, Morale, Stamina, Life, Trade value, Expiration Date.
            public string[] Choices;
            // The game's words on the cell of an expired item with no time until it spoils, and of a rotten item.
            public string Expired, Rotten;
            // The text of the dropdown button with Default.
            public string Sort;
            // The text of the days on a badge, with the placeholder {days}: {days}d in English, {days}天 in Chinese, as
            // the game writes the shelf life ({0}d, {0}天 in SR_UI_ItemTips_6).
            public string DaysLeft;
        }

        // oneUse holds the values of one use of the item: the instance values of a cooked dish, else the config
        // values of its Effect, with one portion. A stat of an item that is not food, or an eat value that rounds to
        // 0, has no number. The trade value is that of the whole stack: config value x uses x count x (1 + appraisal).
        public static Numbers ItemNumbers(EatLogic.Dish oneUse, bool isFood, EatLogic.Factors factors, int configTrade, float tradeUses, int count, float appraisal, DaysLeft days)
        {
            var stats = new int?[4];
            if (isFood)
            {
                var eat = EatLogic.EatValues(oneUse, factors);
                for (var i = 0; i < 4; i++)
                {
                    var v = (int)Math.Round(eat[EatIndex[i]], MidpointRounding.AwayFromZero);
                    if (v != 0) stats[i] = v;
                }
            }
            var trade = (int)Math.Round(configTrade * (double)tradeUses * count * (1.0 + appraisal), MidpointRounding.AwayFromZero);
            return new Numbers { Stats = stats, Trade = trade > 0 ? (int?)trade : null, Days = days };
        }

        // The uses of a stack that the trade counts (Reducer_Web_TradeUI.EffectiveUses, as Better Trade reads it): the
        // part of the uses left, times the config uses; 1 for an item with no use count.
        public static float TradeUses(int useTimes, int maxUseTimes, int cfgUses)
        {
            int uses = cfgUses < 1 ? 1 : cfgUses;
            if (useTimes < 1) return 1f;
            int max = maxUseTimes > 0 ? maxUseTimes : uses;
            return (float)useTimes / max * uses;
        }

        // The cage satiety of an item (Furniture.GetRatCageFoodSatiety): the first instance value of a cooked dish or a
        // ration, else the raw satiety of the config (ValueDisplay1), times the uses left, or with no uses count the
        // effective max uses (the item's own max, else the config uses, at least 1). Null when the satiety is not
        // above 0 or the number rounds to 0: the Rat Cage does not take the item.
        public static int? CageSatiety(float[] instanceValues, float valueDisplay1, int useTimes, int maxUseTimes, int cfgUses)
        {
            float satiety = instanceValues != null && instanceValues.Length > 0 ? instanceValues[0] : valueDisplay1;
            if (!(satiety > 0f)) return null;
            int uses = useTimes >= 1 ? useTimes : Math.Max(1, maxUseTimes > 0 ? maxUseTimes : cfgUses);
            var v = (int)Math.Round((double)satiety * uses, MidpointRounding.AwayFromZero);
            return v > 0 ? (int?)v : null;
        }

        // The badge of the days left: the daysLeft text with {days} filled by whole days, or by one decimal under one day
        // (at least 0.1).
        public static string DaysText(float days, string daysLeft)
        {
            if (days >= 1f) return I18nTexts.Fill(daysLeft, ("days", (int)Math.Floor(days)));
            var tenths = Math.Max(0.1, Math.Round(days, 1, MidpointRounding.AwayFromZero));
            return I18nTexts.Fill(daysLeft, ("days", tenths.ToString("0.0", CultureInfo.InvariantCulture)));
        }

        // The words of the dropdown from the mod texts of one language (src/i18n/): the game's Default text
        // (SR_Web_EventChoice_DefaultTag), the stat names and the trade value label of the preview words, the game's
        // word for the shelf life (UI_BattleBag_6, Expiration Date), the game's expired and rotten words (UI_ItemTips_2,
        // SR_Web_Backpack_Rotten), the mod's word for the button with Default (the game's Sort, 整理, is the word of
        // its Auto Organize), and the text of the days left.
        internal static Words WordsFrom(PreviewLogic.Words words, I18nTextSet texts)
        {
            return new Words
            {
                Choices = new[]
                {
                    texts["sortDefault"],
                    words.Stat[EatLogic.Satiety], words.Stat[EatLogic.Morale], words.Stat[EatLogic.Stamina], words.Stat[EatLogic.Life],
                    words.TradeLabel,
                    texts["expirationDate"],
                },
                Sort = texts["sort"],
                DaysLeft = texts["daysLeft"],
                Expired = texts["expired"],
                Rotten = texts["rotten"],
            };
        }

        // The numbers of two grids of one window (the container tab and the workbench of the cooking window, the
        // storage and the Backpack of the storage window): the items of the first, then the items of the other that
        // the first does not have.
        public static List<KeyValuePair<long, Numbers>> Merge(IReadOnlyList<KeyValuePair<long, Numbers>> first, IReadOnlyList<KeyValuePair<long, Numbers>> other)
        {
            var result = new List<KeyValuePair<long, Numbers>>(first);
            if (other == null) return result;
            var ids = new HashSet<long>();
            foreach (var kv in first) ids.Add(kv.Key);
            foreach (var kv in other)
                if (ids.Add(kv.Key)) result.Add(kv);
            return result;
        }

        // The food sort shows for a fridge, for a storage that links to the cooking window (the cooking tag or Food),
        // and for the bag in the cooking window.
        public static bool IsSortable(bool isFridge, bool linksToCooking, bool isCookingBag) => isFridge || linksToCooking || isCookingBag;
    }
}
