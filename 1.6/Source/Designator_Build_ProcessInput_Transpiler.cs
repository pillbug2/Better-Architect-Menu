using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.ProcessInput))]
    public static class Designator_Build_ProcessInput_Transpiler
    {
        public static bool shouldSkipFloatMenu = false;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var madeFromStuffGetter = AccessTools.PropertyGetter(typeof(BuildableDef), "MadeFromStuff");
            bool patched = false;

            foreach (var code in instructions)
            {
                yield return code;
                if (!patched && code.Calls(madeFromStuffGetter))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Designator_Build_ProcessInput_Transpiler), nameof(ShouldDisplayFloatMenu)));

                    patched = true;
                }
            }
        }
        private static bool ShouldDisplayFloatMenu(bool isMadeFromStuff, Designator_Build designator)
        {
            return isMadeFromStuff && !CheckAndHandleMaterial(designator);
        }
        private static bool CheckAndHandleMaterial(Designator_Build designator)
        {
            if (shouldSkipFloatMenu)
            {
                var selectedMaterial = ArchitectCategoryTab_DesignationTabOnGUI_Patch.selectedMaterial;
                if (selectedMaterial?.def != null)
                {
                    designator.SetStuffDef(selectedMaterial.def);
                    designator.writeStuff = true;
                    return true;
                }
            }
            return false;
        }
    }
}
