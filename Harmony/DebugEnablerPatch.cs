using System.Collections.Generic;
using HarmonyLib;

namespace WeekUnlocker.Harmony
{
    [HarmonyPatch(typeof(DebugEnablerScript))]
    class DebugEnablerPatch
    {
        //  IL_0042: call bool GameGlobals::get_Eighties()
        //  IL_0047: brtrue.s IL_0051
        //  IL_0049: call int32 DateGlobals::get_Week()
        //  IL_004e: ldc.i4.2
	    //  IL_004f: beq.s IL_0089
        [HarmonyTranspiler]
        [HarmonyPatch("Start")]
        static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(x => x.Calls(AccessTools.DeclaredPropertyGetter(typeof(GameGlobals), nameof(GameGlobals.Eighties)))),
                    new CodeMatch(x => x.Branches(out _)),
                    new CodeMatch(x => x.Calls(AccessTools.DeclaredPropertyGetter(typeof(DateGlobals), nameof(DateGlobals.Week)))),
                    new CodeMatch(x => x.LoadsConstant(2)),
                    new CodeMatch(x => x.Branches(out _))
                )
                .RemoveInstructions(5)
                .InstructionEnumeration();
        }
    }
}
