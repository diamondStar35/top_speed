namespace TopSpeed.Drive.Session
{
    // Naming rules for the race\info finish announcements, shared by the single-player and
    // multiplayer sessions so both call a finish the same way.
    //
    // finished1..finished9 announce a numbered position. Whoever crosses the line last hears
    // finishedlast instead of their number, so a three-car race ends first, second, last. That
    // means no clip is ever needed for the tenth position: with the ten-player maximum, tenth is
    // always last.
    //
    // The live "you are in Nth" position callout follows the same rule: youarepos1..youarepos9 for
    // a numbered position, youareposlast for whoever is currently last in the field. Tenth is always
    // last, so youarepos10 is never needed either.
    internal static class RaceInfoSounds
    {
        public const string FinishedLastKey = "race\\info\\finishedlast";
        public const string PositionLastKey = "race\\info\\youareposlast";

        // A solo run has no last place -- position one is simply the win.
        public static bool IsLastPlace(int finishIndex, int totalRacers)
        {
            return totalRacers > 1 && finishIndex == totalRacers - 1;
        }

        public static string NumberedFinishedKey(int finishIndex)
        {
            return $"race\\info\\finished{finishIndex + 1}";
        }

        public static string NumberedPositionKey(int positionIndex)
        {
            return $"race\\info\\youarepos{positionIndex + 1}";
        }
    }
}
