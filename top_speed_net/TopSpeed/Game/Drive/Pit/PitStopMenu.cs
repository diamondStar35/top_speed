using System;
using System.Collections.Generic;
using TopSpeed.Localization;
using TopSpeed.Menu;

namespace TopSpeed.Game
{
    internal static class PitStopMenu
    {
        public const int RefuelChoiceId = 1;
        public const int TiresChoiceId = 2;
        public const int BothChoiceId = 3;

        public static ChoiceDialog Create(Action<ChoiceDialogResult> onResult)
        {
            var items = new Dictionary<int, string>
            {
                [RefuelChoiceId] = LocalizationService.Mark("Refuel"),
                [TiresChoiceId] = LocalizationService.Mark("Tires"),
                [BothChoiceId] = LocalizationService.Mark("Both")
            };

            return new ChoiceDialog(
                LocalizationService.Mark("Pit stop"),
                null,
                items,
                onResult,
                flags: ChoiceDialogFlags.None)
            {
                OpenAsOverlay = true
            };
        }

        public static float GetWorkDurationSeconds(int choiceId)
        {
            return choiceId switch
            {
                RefuelChoiceId => 8f,
                TiresChoiceId => 11f,
                BothChoiceId => 15f,
                _ => 0f
            };
        }
    }
}
