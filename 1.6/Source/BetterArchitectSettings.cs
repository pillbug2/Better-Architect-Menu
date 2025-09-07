using System;
using System.Collections.Generic;
using Verse;

namespace BetterArchitect
{

    public class BetterArchitectSettings : ModSettings
    {
        public static float menuHeight = 330;
        public static bool hideOnSelection = false;
        public static bool rememberSubcategory = false;
        public static float backgroundAlpha = 0.42f;
        public static Dictionary<string, SortSettings> sortSettingsPerCategory = new Dictionary<string, SortSettings>();
        public static Dictionary<string, bool> groupByTechLevelPerCategory = new Dictionary<string, bool>();
        
        public static BetterArchitectMod mod;
        public static void Save()
        {
            mod.GetSettings<BetterArchitectSettings>().Write();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref menuHeight, "menuHeight", 285);
            Scribe_Values.Look(ref hideOnSelection, "hideOnSelection", false);
            Scribe_Values.Look(ref rememberSubcategory, "rememberSubcategory", false);
            Scribe_Values.Look(ref backgroundAlpha, "backgroundAlpha", 0.15f);
            Scribe_Collections.Look(ref sortSettingsPerCategory, "sortSettingsPerCategory", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref groupByTechLevelPerCategory, "groupByTechLevelPerCategory", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                sortSettingsPerCategory ??= new Dictionary<string, SortSettings>();
                groupByTechLevelPerCategory ??= new Dictionary<string, bool>();
            }
            base.ExposeData();
        }
    }
}
