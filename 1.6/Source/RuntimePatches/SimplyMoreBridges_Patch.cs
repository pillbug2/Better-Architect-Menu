using HarmonyLib;
using Verse;

namespace BetterArchitect.RuntimePatches
{
    public class SimplyMoreBridges_Patch : Mod
    {
        const string patchName = "BetterArchitectMod.SimplyMoreBridgesPatch";
        const string packageID = "mlie.simplymorebridges";

        public SimplyMoreBridges_Patch(ModContentPack content) : base(content)
        {
            if (ModsConfig.IsActive(packageID))
            {
                var harmony = new Harmony(patchName);
                harmony.Patch(
                    original: AccessTools.Method(AccessTools.TypeByName("SimplyMoreBridges.GenerateBridges"), "generateBridgeDef"),
                    postfix: new HarmonyMethod(typeof(SimplyMoreBridges_Patch).GetMethod(nameof(Postfix)))
                );
            }
        }

        public static void Postfix(ref TerrainDef __result)
        {
            var category = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail("Ferny_Foundations");
            __result.designationCategory = category;
        }
    }
}
