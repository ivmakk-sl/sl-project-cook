// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
using System.Collections.Generic;
using GameCore.HotUpdate;

namespace SlShared.IconTabs
{
    // The icon of the tab of a furniture from the furniture table of the game (FurnitureIconIndex: the own icon unless
    // it is a default icon, else the icon of the same model, name, or model family). The index is built at the first use
    // and again when the language key of the mod changes, because it compares the names in the display language; the
    // mod passes the key, so the library does not read the display language.
    internal static class FurnitureIcons
    {
        private static FurnitureIconIndex index;
        private static int builtFor;

        // The icon of a furniture config id; tabName gives the short text of the tab.
        public static TabIcon For(int configId, string tabName, int languageKey)
        {
            var config = ConfigManager.Instance;
            if (config == null) return IconChoice.For(null, null, tabName);
            return Index(config, languageKey).For(configId, tabName);
        }

        private static FurnitureIconIndex Index(ConfigManager config, int languageKey)
        {
            if (index != null && builtFor == languageKey) return index;
            var rows = new List<FurnitureRow>();
            var e = config._Config_Furniture_Dict.GetEnumerator();
            while (e.MoveNext())
            {
                var f = e.Current.Value;
                if (f == null) continue;
                rows.Add(new FurnitureRow(f.ID, config.GetLocalTxt(f.Name), f.ResName, f.ICON));
            }
            index = new FurnitureIconIndex(rows);
            builtFor = languageKey;
            return index;
        }
    }
}
