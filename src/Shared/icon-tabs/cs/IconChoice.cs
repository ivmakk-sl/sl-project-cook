// Library copy of shared/icon-tabs 1.0.0. Do not edit: see src/Shared/icon-tabs/VERSION.
namespace SlShared.IconTabs
{
    // The icon of a container tab: an icon path that the page loads as it is, or no icon and a short text.
    internal readonly struct TabIcon
    {
        public readonly string Icon, Text;
        public TabIcon(string icon, string text) { Icon = icon; Text = text; }
    }

    internal static class IconChoice
    {
        // The own icon of the furniture first, then the icon of another furniture with the same name in the display
        // language (sameNameIcon), else no icon. The text is the first word of the name (split at the first space, so
        // a name with no space, such as a Chinese name, is used whole): the tab shows it with no icon, and when the
        // icon does not load.
        public static TabIcon For(string ownIcon, string sameNameIcon, string name)
        {
            var text = FirstWord(name);
            if (!string.IsNullOrEmpty(ownIcon)) return new TabIcon(ownIcon, text);
            if (!string.IsNullOrEmpty(sameNameIcon)) return new TabIcon(sameNameIcon, text);
            return new TabIcon(null, text);
        }

        private static string FirstWord(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var space = name.IndexOf(' ');
            return space > 0 ? name.Substring(0, space) : name;
        }
    }
}
