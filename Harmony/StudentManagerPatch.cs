using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using HarmonyLib.Tools;
using System.Reflection.Emit;

namespace WeekUnlocker.Harmony
{
    [HarmonyPatch(typeof(StudentManagerScript))]
    class StudentManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        static void AwakePrefix(StudentManagerScript __instance)
        {
            __instance.WeekLimit = WeekUnlockerMod.WeekLimit;
        }
        //	IL_0024: ldc.i4.s 11
        //  IL_0026: ble.s IL_002f
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(StudentManagerScript.SpawnStudent))]
        static IEnumerable<CodeInstruction> SpawnStudentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(x => x.LoadsConstant(11)),
                    new CodeMatch(x => x.Branches(out _))
                )
                .RemoveInstruction()
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.LoadField(typeof(StudentManagerScript), nameof(StudentManagerScript.Week)),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.LoadField(typeof(StudentManagerScript), nameof(StudentManagerScript.WeekLimit)),
                    new CodeInstruction(OpCodes.Add)
                )
                .InstructionEnumeration();
        }
    }
}
