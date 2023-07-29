using System.Collections.Generic;
using HarmonyLib;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib.Tools;

namespace WeekUnlocker.Harmony
{
    [HarmonyPatch(typeof(CalendarScript))]
    class CalendarScriptPatch
    {
        //  IL_0156: call int32 DateGlobals::get_Week()
        //  IL_015b: ldc.i4.2
        //  IL_015c: ble.s IL_016e
        //  IL_015e: ldstr "Save file had to be deleted because 80s and 202X got mixed up."
        //  IL_0163: call void[UnityEngine.CoreModule] UnityEngine.Debug::Log(object)
        //  IL_0168: ldarg.0
        //  IL_0169: call instance void CalendarScript::ResetSaveFile()
        /// <summary>
        /// Removes code to reset the week if 20XX mode gets past week two.
        /// Also adds code to genericise arbitrary/modded date labels.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch("Start")]
        static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(x => x.Branches(out _)),
                    new CodeMatch(x => x.Calls(AccessTools.DeclaredPropertyGetter(typeof(DateGlobals), nameof(DateGlobals.Week))), name: "JumpTarget"),
                    new CodeMatch(x => x.LoadsConstant(2)),
                    new CodeMatch(x => x.Branches(out _)),
                    new CodeMatch(OpCodes.Ldstr),
                    new CodeMatch(x => x.Calls(AccessTools.Method(typeof(Debug), nameof(Debug.Log), new System.Type[] { typeof(object) }))),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(x => x.Calls(AccessTools.Method(typeof(CalendarScript), nameof(CalendarScript.ResetSaveFile))))
                )
                .ThrowIfInvalid("StartTranspiler failed to find first injection site!")
                .RemoveInstructions(8);
                matcher.AddLabels(
                    matcher.NamedMatch("JumpTarget").ExtractLabels()
                );
        //  IL_0353: call int32 DateGlobals::get_Week()
        //  IL_0358: ldc.i4.1
        //  IL_0359: bne.un IL_03e9
        //  IL_035e: ldarg.0
        //  IL_035f: ldfld class UILabel[] CalendarScript::DayNumber
        //  UNTIL
        //  IL_047a: ldarg.0
        //  IL_047b: ldfld class [UnityEngine.CoreModule] UnityEngine.GameObject CalendarScript::AmaiButton
        //  IL_0480: ldc.i4.1
        //  IL_0481: callvirt instance void[UnityEngine.CoreModule] UnityEngine.GameObject::SetActive(bool)

        //  Replace with:
        //  ldarg.0
        //  call CreateDayNumbers()
        //  void CreateDayNumbers(this CalendarScript) {
        //      // that code I wrote on Discord goes here
        //  }
            int endPosition = matcher.MatchEndForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(x => x.LoadsField(AccessTools.DeclaredField(typeof(CalendarScript), nameof(CalendarScript.AmaiButton)))),
                new CodeMatch(x => x.LoadsConstant(1)),
                new CodeMatch(x => x.Calls(AccessTools.DeclaredMethod(typeof(UnityEngine.GameObject), nameof(UnityEngine.GameObject.SetActive))))
                ).Pos;
            return matcher
                .RemoveInstructionsInRange(
                    matcher.End().MatchStartBackwards(
                        new CodeMatch(x => x.Calls(AccessTools.DeclaredPropertyGetter(typeof(DateGlobals), nameof(DateGlobals.Week)))),
                        new CodeMatch(x => x.LoadsConstant(1)),
                        new CodeMatch(x => x.Branches(out _)),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(x => x.LoadsField(AccessTools.DeclaredField(typeof(CalendarScript), nameof(CalendarScript.DayNumber))))
                    ).Pos,
                    endPosition
                )
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    CodeInstruction.Call(typeof(CalendarScriptPatch), nameof(CalendarScriptPatch.CreateDayNumbers))
                )
                .InstructionEnumeration();
        }

        static void CreateDayNumbers(CalendarScript calendarScript)
        {
            // Realistically you should only need to use 4 of these for all 10 weeks, at most.
            // But I'll give you a whole year (excluding leap years) just in case.
            // If someone adds enough weeks that they need to take into account leap years they can make a PR to support it themselves.
            /// What's the length of each month? January through December, February is always 28 days.
            int[] monthLengths = new int[12] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
            string[] monthNames = new string[12] { "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER" };
            for (int day = 0; day < 7; day++)
            {
                // We have to calculate the days since the (zero-indexed) starting day by converting the current week to days and adding our day offset.
                // We have to convert the 1-indexed Week variable to be zero-indexed (subtract one before multiplying)
                // Then we use the modulo operator to handle wrapping around at the end of the month.
                // We subtract 1 inside the modulo and add 1 outside so that it goes from 1 to 30 instead of 0 to 29.
                int TotalDay = WeekUnlockerMod.StartingDay + day + 7 * (DateGlobals.Week - 1);
                int monthDay = TotalDay;
                int monthIndex = WeekUnlockerMod.StartingMonth;
                int currentYear = WeekUnlockerMod.StartingYear;
                while (monthDay > monthLengths[monthIndex])
                {
                    monthDay -= monthLengths[monthIndex];
                    monthIndex++;
                    if (monthIndex >= monthLengths.Length)
                    {
                        Debug.Log("Ran out of month lengths, wrapping around to the start! Did you actually mean to add more than a year's worth of gameplay?");
                        monthIndex %= monthLengths.Length;
                        currentYear++;
                    }
                }
                Debug.Log($"Week {DateGlobals.Week} day {day} is day {monthDay} out of {monthLengths[monthIndex]} in {monthNames[monthIndex]} of {currentYear}, {TotalDay} days have passed overall.");
                // DayNumber is 1-indexed.
                calendarScript.DayNumber[day + 1].text = $"{monthDay}";
                if (DateGlobals.Weekday == (System.DayOfWeek)day)
                {
                    calendarScript.MonthLabel.text = monthNames[monthIndex];
                    calendarScript.YearLabel.text = $"{currentYear}";
                }
            }
        }

        /// <summary>
        /// Removes code that disables skipping the week if it's Week 2.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(CalendarScript.ChangeDayColor))]
        static IEnumerable<CodeInstruction> ChangeDayColorTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_0, name: "JumpTarget"),
                    new CodeMatch(x => x.LoadsField(AccessTools.Field(typeof(CalendarScript), nameof(CalendarScript.Eighties)))),
                    new CodeMatch(x => x.Branches(out _)),
                    new CodeMatch(x => x.Calls(AccessTools.DeclaredPropertyGetter(typeof(DateGlobals), nameof(DateGlobals.Week)))),
                    new CodeMatch(x => x.LoadsConstant(2)),
                    new CodeMatch(x => x.Branches(out _)),
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(x => x.LoadsField(AccessTools.Field(typeof(CalendarScript), nameof(CalendarScript.SkipButton)))),
                    new CodeMatch(x => x.LoadsConstant(0)),
                    new CodeMatch(x => x.Calls(AccessTools.Method(typeof(GameObject), nameof(GameObject.SetActive))))
                )
                .ThrowIfInvalid("ChangeDayColor failed to find injection site!");
            return matcher
                .RemoveInstructions(10)
                .AddLabels(matcher.NamedMatch("JumpTarget").ExtractLabels())
                .InstructionEnumeration();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(CalendarScript.ChangeDayColor))]
        static void ChangeDayColorPrefix(CalendarScript __instance)
        {
            if (!__instance.Eighties) CreateDayNumbers(__instance);
        }
    }
}
