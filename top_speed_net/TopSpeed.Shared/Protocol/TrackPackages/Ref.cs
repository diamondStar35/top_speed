using System;

namespace TopSpeed.Protocol
{
    public enum RoomTrackSelectionKind : byte
    {
        None = 0,
        BuiltIn = 1,
        CustomPackage = 2
    }

    public sealed class TrackPackageRef
    {
        public const string RandomBuiltInAnyKey = "$random:builtin:any";
        public const string RandomBuiltInRaceKey = "$random:builtin:race";
        public const string RandomBuiltInAdventureKey = "$random:builtin:adventure";

        public RoomTrackSelectionKind Kind = RoomTrackSelectionKind.None;
        public string BuiltInTrackKey = string.Empty;
        public string TrackId = string.Empty;
        public string Version = string.Empty;
        public string Hash = string.Empty;

        public bool IsBuiltIn => Kind == RoomTrackSelectionKind.BuiltIn;
        public bool IsCustomPackage => Kind == RoomTrackSelectionKind.CustomPackage;
        public bool IsRandomBuiltIn => IsRandomBuiltInTrackKey(BuiltInTrackKey);

        public static TrackPackageRef BuiltIn(string trackKey)
        {
            return new TrackPackageRef
            {
                Kind = RoomTrackSelectionKind.BuiltIn,
                BuiltInTrackKey = (trackKey ?? string.Empty).Trim()
            };
        }

        public static TrackPackageRef RandomBuiltInAny()
        {
            return BuiltIn(RandomBuiltInAnyKey);
        }

        public static TrackPackageRef RandomBuiltInRace()
        {
            return BuiltIn(RandomBuiltInRaceKey);
        }

        public static TrackPackageRef RandomBuiltInAdventure()
        {
            return BuiltIn(RandomBuiltInAdventureKey);
        }

        public static TrackPackageRef Custom(string trackId, string version, string hash)
        {
            return new TrackPackageRef
            {
                Kind = RoomTrackSelectionKind.CustomPackage,
                TrackId = (trackId ?? string.Empty).Trim(),
                Version = (version ?? string.Empty).Trim(),
                Hash = NormalizeHash(hash)
            };
        }

        public static TrackPackageRef Clone(TrackPackageRef? track)
        {
            if (track == null)
                return BuiltIn(string.Empty);

            return track.IsCustomPackage
                ? Custom(track.TrackId, track.Version, track.Hash)
                : BuiltIn(track.BuiltInTrackKey);
        }

        public static TrackPackageRef Normalize(TrackPackageRef? track)
        {
            return Clone(track);
        }

        public static bool AreEqual(TrackPackageRef? left, TrackPackageRef? right)
        {
            var a = Clone(left);
            var b = Clone(right);
            if (a.Kind != b.Kind)
                return false;

            if (a.IsCustomPackage)
            {
                return string.Equals(a.TrackId, b.TrackId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(a.Version, b.Version, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(NormalizeHash(a.Hash), NormalizeHash(b.Hash), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(a.BuiltInTrackKey, b.BuiltInTrackKey, StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeHash(string hash)
        {
            return (hash ?? string.Empty).Trim().ToLowerInvariant();
        }

        public static bool IsRandomBuiltInTrackKey(string trackKey)
        {
            var normalized = (trackKey ?? string.Empty).Trim();
            return string.Equals(normalized, RandomBuiltInAnyKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, RandomBuiltInRaceKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, RandomBuiltInAdventureKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}
