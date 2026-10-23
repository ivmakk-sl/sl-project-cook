// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SlShared.IconTabs
{
    // One row of the furniture table of the game: the config id, the name in the display language, the model
    // (Config_Furniture.ResName), and the icon path (Config_Furniture.ICON, empty for most furniture found in the world).
    internal readonly struct FurnitureRow
    {
        public readonly int Id;
        public readonly string Name, Model, Icon;
        public FurnitureRow(int id, string name, string model, string icon) { Id = id; Name = name; Model = model; Icon = icon; }
    }

    // The icon of a furniture from the furniture table. The game sets a few default icons on dozens of unrelated
    // furniture (in game 1.1.18599 a bed on the Medium Storage Rack, a shelf on the Refrigerator), so an icon that
    // furniture of DefaultIconNames or more names share counts as no icon. The order: the own icon, the icon of the same
    // model, of the same name, of the same model family (the model with no variant), else the first word of the name.
    internal sealed class FurnitureIconIndex
    {
        public const int DefaultIconNames = 10;

        // The variant of a model: _S1_M02, _S1A_M01.
        private static readonly Regex Variant = new Regex(@"_S\d+[A-Z]?_M\d+$");

        private readonly Dictionary<int, FurnitureRow> byId = new Dictionary<int, FurnitureRow>();
        private readonly HashSet<string> defaults = new HashSet<string>();
        private readonly Dictionary<string, string> byModel = new Dictionary<string, string>();
        private readonly Dictionary<string, string> byName = new Dictionary<string, string>();
        private readonly Dictionary<string, string> byFamily = new Dictionary<string, string>();

        // rows in the order of the table: the first real icon of a model, a name, or a family is its icon.
        public FurnitureIconIndex(IEnumerable<FurnitureRow> rows)
        {
            var namesOfIcon = new Dictionary<string, HashSet<string>>();
            var list = new List<FurnitureRow>();
            foreach (var r in rows)
            {
                list.Add(r);
                byId[r.Id] = r;
                if (string.IsNullOrEmpty(r.Icon)) continue;
                if (!namesOfIcon.TryGetValue(r.Icon, out var names)) namesOfIcon[r.Icon] = names = new HashSet<string>();
                names.Add(r.Name ?? "");
            }
            foreach (var kv in namesOfIcon)
                if (kv.Value.Count >= DefaultIconNames) defaults.Add(kv.Key);
            foreach (var r in list)
            {
                if (!IsReal(r.Icon)) continue;
                Add(byModel, r.Model, r.Icon);
                Add(byName, r.Name, r.Icon);
                Add(byFamily, ModelFamily(r.Model), r.Icon);
            }
        }

        public bool IsDefault(string icon) => icon != null && defaults.Contains(icon);

        private bool IsReal(string icon) => !string.IsNullOrEmpty(icon) && !defaults.Contains(icon);

        private static void Add(Dictionary<string, string> map, string key, string icon)
        {
            if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key)) map[key] = icon;
        }

        private static string Find(Dictionary<string, string> map, string key) =>
            !string.IsNullOrEmpty(key) && map.TryGetValue(key, out var icon) ? icon : null;

        // tabName gives the short text of the tab.
        public TabIcon For(int configId, string tabName)
        {
            if (!byId.TryGetValue(configId, out var r)) return IconChoice.For(null, null, tabName);
            var icon = IsReal(r.Icon) ? r.Icon
                : Find(byModel, r.Model) ?? Find(byName, r.Name) ?? Find(byFamily, ModelFamily(r.Model));
            return IconChoice.For(icon, null, tabName);
        }

        public static string ModelFamily(string model) => string.IsNullOrEmpty(model) ? "" : Variant.Replace(model, "");
    }
}
