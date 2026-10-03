using System;
using GameCore.HotUpdate.Battle.Logic;

namespace ProjectCook
{
    // The talent and buff ratios that the quality and EXP math need, read once for each refresh: the game's own
    // method gives the sum of the owned talents and any temporary buff. A failed read counts as 0 and warns once.
    internal struct TalentInputs
    {
        public float PerfectQuality, TagQuality, RottenReduce, ExpRatio, Appraisal;

        private static bool warned;

        public static TalentInputs Read()
        {
            return new TalentInputs
            {
                PerfectQuality = Ratio("Buff/AE_PerfectQualityMultiplier"),
                TagQuality = Ratio("Buff/AE_CookTagQualityBonus"),
                RottenReduce = Ratio("Buff/AE_CookRottenPenaltyReduce"),
                ExpRatio = Ratio("Buff/AE_CookExpMultiplier"),
                Appraisal = ReadAppraisal(),
            };
        }

        // The appraisal talents of the trade (Shrewd Appraisal, Bargaining): the game uses this sum for the real trade.
        public static float ReadAppraisal() => Ratio("Buff/AE_StrangerTradeAppraisalBonus");

        private static float Ratio(string key)
        {
            try { return Furniture.GetTalentEffectRatio(key); }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning($"talent read of {key} failed, treated as 0: {e.Message}");
                }
                return 0f;
            }
        }
    }
}
