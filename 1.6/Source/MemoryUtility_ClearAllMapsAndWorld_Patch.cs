using HarmonyLib;
using Verse.Profile;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    public static class MemoryUtility_ClearAllMapsAndWorld_Patch
    {
        public static void Prefix()
        {
            ArchitectCategoryTab_DesignationTabOnGUI_Patch.Reset();
        }
    }
}
