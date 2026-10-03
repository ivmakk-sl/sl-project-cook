using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;

namespace ProjectCook
{
    // The eat factors of the leading role for EatLogic: its talents and the difficulty, not its temporary buffs.
    // A talent level is a buff of the character (TalentComponent adds the BuffID of each level that it reaches, and
    // the lower levels keep their buffs), so the read takes the active buffs (BuffComponent.buffIdMap) whose id is the
    // BuffID of a talent row, and maps the effect of each one. A failed read counts as no factor and warns once.
    internal static class EatInputs
    {
        private const int FoodTypes = 12;

        private static readonly AttrName[] AddRatios =
        {
            AttrName.SatietyAdd_Ratio, AttrName.MoraleAdd_Ratio, AttrName.StaminaAdd_Ratio, AttrName.HealthAdd_Ratio, AttrName.VitalityAdd_Ratio,
        };

        private static Dictionary<int, Config_Talent> talentByBuff;
        private static object talentByBuffSource;
        private static PropertyInfo[] satietyFood, moraleFood;
        private static bool warned;

        public static EatLogic.Factors Read(out string log)
        {
            var factors = new EatLogic.Factors();
            var text = new StringBuilder();
            try
            {
                var config = ConfigManager.Instance;
                var role = BaseSingleton<BattleLogicWorld>.Instance?._AgentManager?.GetLeadingRole();
                if (config == null || role == null)
                {
                    log = "no role";
                    return factors;
                }

                var attributes = AgentTools.GetAgentComponent<AttributeComponent>(role);
                if (attributes != null)
                {
                    factors.BasePermille = new int[5];
                    for (var i = 0; i < 5; i++) factors.BasePermille[i] = attributes.GetBaseValue_Int(AddRatios[i]);
                    text.Append("base=").Append(string.Join("/", factors.BasePermille));
                }

                factors.LifeDelta = DifficultyTools.GetVitalityItemAddDelta();
                if (factors.LifeDelta != 0f)
                {
                    var level = config.Get_Config_DifficultyParam((int)DifficultyParamType.Survival, DifficultyTools.GetLevel(DifficultyParamType.Survival));
                    // The game's level name with its parameter, for example "Strict (Survival Consumption)".
                    if (level != null) factors.DifficultyName = config.GetLocalTxt(level.LevelName) + " (" + config.GetLocalTxt(level.ParamName) + ")";
                }
                text.Append(" life=").Append(factors.LifeDelta).Append(' ').Append(factors.DifficultyName);

                var buffs = AgentTools.GetAgentComponent<BuffComponent>(role)?.buffIdMap;
                if (buffs != null)
                {
                    var talents = TalentByBuff(config);
                    var e = buffs.GetEnumerator();
                    while (e.MoveNext())
                    {
                        var buffId = e.Current.Key;
                        if (!talents.TryGetValue(buffId, out var row)) continue;
                        var buff = config.Get_Config_Buff(buffId);
                        // A fixed talent has no end (BuffDuring -1). A talent with a time (Leisure Time, Eat Up for
                        // 24 hours, Rich Diet) is a temporary buff, which the eat values leave out.
                        if (buff == null || buff.BuffDuring >= 0f) continue;
                        var effect = config.Get_Config_Effect(buff.EffectConfigId);
                        if (effect == null) continue;
                        var count = e.Current.Value?.Count ?? 1;
                        for (var n = 0; n < Math.Max(1, count); n++)
                        {
                            var talent = ToTalent(effect, config.GetLocalTxt(row.Name));
                            if (talent == null) continue;
                            factors.Talents.Add(talent);
                            text.Append(" | ").Append(talent.Name).Append(' ').Append(effect.ConfigName).Append(' ').Append(Describe(talent));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    Plugin.Log.LogWarning($"Project Cook: the eat talents could not be read, the dish cards show the values without them: {e.Message}");
                }
                text.Append(" failed: ").Append(e.Message);
            }
            log = text.ToString();
            return factors;
        }

        // The talent of one effect, or null when the effect does not change the eat path.
        private static EatLogic.Talent ToTalent(Config_Effect effect, string name)
        {
            var t = new EatLogic.Talent { Name = name };
            var any = false;
            switch (effect.ConfigName)
            {
                case "Buff/AE_CookDishNourish": t.Nourish = effect.FloatValue0; any = true; break;
                case "Buff/AE_CookEatPerfectMorale": t.PerfectMorale = effect.FloatValue0; any = true; break;
                // AE_AddConditionAttr_Float adds (int)(FloatValue0 x 1000) to the Satiety Add ratio.
                case "Buff/Efficient_Nutrition": t.EfficientPermille = (int)(effect.FloatValue0 * 1000f); any = true; break;
                case "Buff/Buff_304310_ComfortFood": t.ComfortPermille = (int)(effect.FloatValue0 * 1000f); any = true; break;
            }
            float[] add = { effect.BuffSatietyAdd, effect.BuffMoraleAdd, effect.BuffStaminaAdd, 0f, effect.BuffVitalityAdd };
            if (Array.Exists(add, v => v != 0f))
            {
                t.AddPermille = Array.ConvertAll(add, v => (int)(v * 1000f));
                any = true;
            }
            t.SatietyFood = FoodRatios(effect, ref satietyFood, "BuffSatietyFood{0}Add_Ratio");
            t.MoraleFood = FoodRatios(effect, ref moraleFood, "EF_BuffMoraleFood{0}Add_Ratio");
            any |= t.SatietyFood != null || t.MoraleFood != null;
            return any ? t : null;
        }

        // The food-type ratios of an effect by SubCategory, or null when all are 0. The config has one field for each
        // food type, so the properties are found by name once.
        private static float[] FoodRatios(Config_Effect effect, ref PropertyInfo[] properties, string format)
        {
            if (properties == null)
            {
                properties = new PropertyInfo[FoodTypes];
                for (var i = 1; i < FoodTypes; i++) properties[i] = typeof(Config_Effect).GetProperty(string.Format(format, i));
            }
            float[] ratios = null;
            for (var i = 1; i < FoodTypes; i++)
            {
                if (properties[i] == null) continue;
                var v = (float)properties[i].GetValue(effect);
                if (v == 0f) continue;
                ratios = ratios ?? new float[FoodTypes];
                ratios[i] = v;
            }
            return ratios;
        }

        private static Dictionary<int, Config_Talent> TalentByBuff(ConfigManager config)
        {
            var source = config._Config_Talent_Dict;
            if (talentByBuff != null && ReferenceEquals(source, talentByBuffSource)) return talentByBuff;
            var map = new Dictionary<int, Config_Talent>();
            var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                var row = e.Current.Value;
                if (row != null && row.BuffID > 0) map[row.BuffID] = row;
            }
            talentByBuff = map;
            talentByBuffSource = source;
            return map;
        }

        private static string Describe(EatLogic.Talent t)
        {
            var parts = new List<string>();
            if (t.Nourish != 0f) parts.Add("nourish=" + t.Nourish);
            if (t.PerfectMorale != 0f) parts.Add("perfectMorale=" + t.PerfectMorale);
            if (t.EfficientPermille != 0) parts.Add("efficient=" + t.EfficientPermille);
            if (t.ComfortPermille != 0) parts.Add("comfort=" + t.ComfortPermille);
            if (t.AddPermille != null) parts.Add("add=" + string.Join("/", t.AddPermille));
            if (t.SatietyFood != null) parts.Add("satFood=" + string.Join("/", t.SatietyFood));
            if (t.MoraleFood != null) parts.Add("morFood=" + string.Join("/", t.MoraleFood));
            return string.Join(" ", parts);
        }
    }
}
