namespace TopSpeed.Bots
{
    /// <summary>
    /// Difficulty is skill, not recklessness: every profile avoids collisions competently and
    /// none of them aim at another car. What changes is how much of the car's real envelope the
    /// driver dares to use, how quickly it reacts, and how tidy its line is.
    /// </summary>
    public readonly struct BotSkillProfile
    {
        public BotSkillProfile(
            float gripConfidence,
            float brakeConfidence,
            float reactionSeconds,
            float headwaySeconds,
            float lineWeight,
            float straightPace,
            float overtakeBoldness)
        {
            GripConfidence = gripConfidence;
            BrakeConfidence = brakeConfidence;
            ReactionSeconds = reactionSeconds;
            HeadwaySeconds = headwaySeconds;
            LineWeight = lineWeight;
            StraightPace = straightPace;
            OvertakeBoldness = overtakeBoldness;
        }

        /// <summary>Fraction of the corridor-following limit the driver is willing to use.</summary>
        public float GripConfidence { get; }

        /// <summary>Fraction of full braking authority assumed when planning the braking point.</summary>
        public float BrakeConfidence { get; }

        /// <summary>Control lag; also the interval between heavy re-plans.</summary>
        public float ReactionSeconds { get; }

        /// <summary>Car-following time headway.</summary>
        public float HeadwaySeconds { get; }

        /// <summary>How strongly the driver seeks the ideal line when traffic allows.</summary>
        public float LineWeight { get; }

        /// <summary>Fraction of top speed used where the road imposes no limit.</summary>
        public float StraightPace { get; }

        /// <summary>Willingness to commit to a lane change for a small gain.</summary>
        public float OvertakeBoldness { get; }

        public static BotSkillProfile For(BotDrivingDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDrivingDifficulty.Easy:
                    return new BotSkillProfile(0.80f, 0.75f, 0.35f, 1.8f, 0.30f, 0.90f, 0.55f);
                case BotDrivingDifficulty.Hard:
                    return new BotSkillProfile(0.98f, 0.97f, 0.07f, 1.00f, 1.00f, 1.00f, 1.00f);
                default:
                    return new BotSkillProfile(0.90f, 0.88f, 0.18f, 1.30f, 0.70f, 0.96f, 0.80f);
            }
        }
    }
}
