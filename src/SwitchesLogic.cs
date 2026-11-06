using System.Collections.Generic;

namespace ProjectCook
{
    // The page features of the switches that are on, free of BepInEx: the page script reads them from each setData
    // (PageJson.DataJson), so a switch change sends a new list and the page shows or hides the part.
    public static class SwitchesLogic
    {
        public struct Values
        {
            public bool DishPreview, CodexMarks, UnknownWithIngredients, UnknownWithoutIngredients, SeparatePieces, DimUncookable;
        }

        public static string[] PageFeatures(Values v)
        {
            var features = new List<string>();
            if (v.CodexMarks) features.Add("codexMarks");
            if (v.CodexMarks && (v.UnknownWithIngredients || v.UnknownWithoutIngredients)) features.Add("unknownCodexRecipes");
            if (v.SeparatePieces) features.Add("separatePieces");
            if (v.DimUncookable) features.Add("dimUncookable");
            // The portion switch is part of the dish preview.
            if (v.DishPreview) features.Add("portionSwitch");
            return features.ToArray();
        }

        // The food sort draws from the sort data that the page keeps, and with FoodSort off C# sends no new data: the
        // switch off sends no data (setSortData(null)), so the next pass of each window takes the sort off.
        public const string NoSortJson = "null";

        public static bool ClearsSort(string key, bool value) => key == "FoodSort" && !value;

        public static bool Same(string[] a, string[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
