using System.Collections.Generic;
using Verse;

namespace BetterArchitect
{

    public class BetterArchitectSettings : ModSettings
    {
        public static float menuHeight = 285;
        public static bool hideOnSelection = false;
        public static bool groupByTechLevel = false;
        public static float backgroundAlpha = 0f;
        public static Dictionary<string, SortSettings> sortSettingsPerCategory = new Dictionary<string, SortSettings>();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref menuHeight, "menuHeight", 285);
            Scribe_Values.Look(ref hideOnSelection, "hideOnSelection", false);
            Scribe_Values.Look(ref groupByTechLevel, "groupByTechLevel", false);
            Scribe_Values.Look(ref backgroundAlpha, "backgroundAlpha", 0f);
            Scribe_Collections.Look(ref sortSettingsPerCategory, "sortSettingsPerCategory", LookMode.Value, LookMode.Deep);
            base.ExposeData();
        }
    }
}
