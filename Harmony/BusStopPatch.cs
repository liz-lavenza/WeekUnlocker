using System.Collections.Generic;
using HarmonyLib;
using System.Reflection.Emit;

namespace WeekUnlocker.Harmony
{
    [HarmonyPatch(typeof(BusStopScript))]
    class BusStopPatch
    {
        //	IL_0024: ldc.i4.s 11
        //  IL_0026: ble.s IL_002f
        [HarmonyTranspiler]
        [HarmonyPatch("ExitCutscene")]
        static IEnumerable<CodeInstruction> ExitCutsceneTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(x => x.LoadsConstant(2)),
                    new CodeMatch(x => x.Calls(AccessTools.DeclaredPropertySetter(typeof(DateGlobals), nameof(DateGlobals.Week))))
                )
                .RemoveInstruction()
                .Insert(
                    CodeInstruction.Call(typeof(DateGlobals), AccessTools.DeclaredPropertyGetter(typeof(DateGlobals), nameof(DateGlobals.Week)).Name),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Add)
                )
                .InstructionEnumeration();
        }
    }
}
