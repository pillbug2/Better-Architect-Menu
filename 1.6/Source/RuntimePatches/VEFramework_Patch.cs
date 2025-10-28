using HarmonyLib;
using Verse;

namespace BetterArchitect.RuntimePatches
{
    // Inheriting from Mod is necessary as Outposts.OutpostsMod also inherits Mod and will otherwise execute FindOutposts() before the patch is applied.
    public class VEFramework_Patch : Mod
    {
        /* Field descriptions:
         * patchName    A meaningful name for this patch in Harmony.
         * packageID    The packageID of the mod which must be loaded.
         */
        const string patchName = "BetterArchitectMod.VEFrameworkPatch";
        const string packageID = "oskarpotocki.vanillafactionsexpanded.core";

        public VEFramework_Patch(ModContentPack content) : base(content) {
            // Only execute if mod is present.
            if (ModsConfig.IsActive(packageID))
            {
                // Set Postfix() to execute after Outposts.OutpostsMod.FindOutposts() executes.
                var harmony = new Harmony(patchName);
                harmony.Patch(
                    original : AccessTools.Method(AccessTools.TypeByName("Outposts.OutpostsMod"), "FindOutposts"),
                    postfix  : new HarmonyMethod(typeof(VEFramework_Patch).GetMethod(nameof(Postfix)))
                );
            }
        }

        // Assign VEF_OutpostDeliverySpot to a new category.
        public static void Postfix()
        {
            var category = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("Ferny_ColonyLocations");
            var thing = DefDatabase<BuildableDef>.GetNamedSilentFail("VEF_OutpostDeliverySpot");
            thing.designationCategory = category;
        }
    }
}
