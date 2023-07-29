using MelonLoader;

namespace WeekUnlocker
{
    public class WeekUnlockerMod : MelonMod
    {
        // HEY! If you want to change the starting date for your mod, override OnLateInitializeMelon on your mod
        // and set these variables there, via WeekUnlockerMod.StartingDay/etc.
        // Don't edit this mod to change it!
        /// What day of the month (one-indexed) is the Sunday of the first week of the game? Default is 2.
        public static int StartingDay = 2;
        /// Which month (zero-indexed, 0 through 11) is the first week of the game in? Default is April (3).
        public static int StartingMonth = 3;
        /// What year is the game set in?
        public static int StartingYear = 2025;
    }
}
