using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TopSpeed.Tracks.Map;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Sectors
{
    public sealed class TrackSectorRuleManager
    {
        private readonly Dictionary<string, TrackSectorRules> _rulesBySector;
        private readonly TrackPortalManager _portalManager;

        public TrackSectorRuleManager(IEnumerable<TrackSectorDefinition> sectors, TrackPortalManager portalManager)
        {
            _portalManager = portalManager ?? throw new ArgumentNullException(nameof(portalManager));
            _rulesBySector = new Dictionary<string, TrackSectorRules>(StringComparer.OrdinalIgnoreCase);

            if (sectors == null)
                return;

            foreach (var sector in sectors)
            {
                if (sector == null)
                    continue;
                _rulesBySector[sector.Id] = BuildRules(sector);
            }
        }

        public IReadOnlyCollection<TrackSectorRules> Rules => _rulesBySector.Values;

        public bool TryGetRules(string sectorId, out TrackSectorRules rules)
        {
            rules = null!;
            if (string.IsNullOrWhiteSpace(sectorId))
                return false;
            return _rulesBySector.TryGetValue(sectorId.Trim(), out rules!);
        }

        public bool IsClosed(string sectorId)
        {
            return TryGetRules(sectorId, out var rules) && rules.IsClosed;
        }

        public bool IsRestricted(string sectorId)
        {
            return TryGetRules(sectorId, out var rules) && rules.IsRestricted;
        }

        public bool AllowsEntry(string sectorId, string? portalId, MapDirection? direction)
        {
            if (!TryGetRules(sectorId, out var rules))
                return true;
            if (rules.IsClosed)
                return false;
            if (rules.IsRestricted)
                return false;
            return Allows(rules.AllowedEntryPortals, rules.DeniedEntryPortals, rules.AllowedEntryDirections, rules.DeniedEntryDirections, portalId, direction);
        }

        public bool AllowsExit(string sectorId, string? portalId, MapDirection? direction)
        {
            if (!TryGetRules(sectorId, out var rules))
                return true;
            if (rules.IsClosed)
                return false;
            return Allows(rules.AllowedExitPortals, rules.DeniedExitPortals, rules.AllowedExitDirections, rules.DeniedExitDirections, portalId, direction);
        }

        public bool TryGetSpeedLimits(string sectorId, out float? minSpeedKph, out float? maxSpeedKph)
        {
            minSpeedKph = null;
            maxSpeedKph = null;
            if (!TryGetRules(sectorId, out var rules))
                return false;
            minSpeedKph = rules.MinSpeedKph;
            maxSpeedKph = rules.MaxSpeedKph;
            return minSpeedKph.HasValue || maxSpeedKph.HasValue;
        }

        public bool TryGetPrimaryPortal(
            string sectorId,
            MapDirection? heading,
            out PortalDefinition? portal,
            out float? portalDeltaDegrees)
        {
            portal = null;
            portalDeltaDegrees = null;
            if (string.IsNullOrWhiteSpace(sectorId))
                return false;

            var portals = _portalManager.GetPortalsForSector(sectorId);
            if (portals.Count == 0 || !heading.HasValue)
                return false;

            var headingDegrees = ToHeadingDegrees(heading.Value);
            PortalDefinition? best = null;
            float? bestDelta = null;

            foreach (var candidate in portals)
            {
                if (candidate == null)
                    continue;
                var delta = ResolveDelta(candidate, headingDegrees);
                if (!delta.HasValue)
                    continue;
                if (!bestDelta.HasValue || delta.Value < bestDelta.Value)
                {
                    bestDelta = delta;
                    best = candidate;
                }
            }

            if (best == null)
                return false;

            portal = best;
            portalDeltaDegrees = bestDelta;
            return true;
        }

        private static TrackSectorRules BuildRules(TrackSectorDefinition sector)
        {
            var metadata = sector.Metadata;
            var isClosed = (sector.Flags & TrackSectorFlags.Closed) != 0 || GetBool(metadata, "closed", "blocked");
            var isRestricted = (sector.Flags & TrackSectorFlags.Restricted) != 0 || GetBool(metadata, "restricted", "no_entry", "noentry");
            var requiresStop = GetBool(metadata, "stop", "stop_required", "stopline");
            var requiresYield = GetBool(metadata, "yield", "give_way", "giveway");

            var maxSpeed = GetFloat(metadata, "speed_limit", "speed_limit_kph", "max_speed", "max_speed_kph");
            var minSpeed = GetFloat(metadata, "min_speed", "min_speed_kph");

            var allowedEntryPortals = ParseIdList(metadata, "allowed_entries", "allowed_entry_portals", "entry_portals", "entry_ports");
            var deniedEntryPortals = ParseIdList(metadata, "denied_entries", "deny_entry_portals", "blocked_entry_portals");
            var allowedExitPortals = ParseIdList(metadata, "allowed_exits", "allowed_exit_portals", "exit_portals", "exit_ports");
            var deniedExitPortals = ParseIdList(metadata, "denied_exits", "deny_exit_portals", "blocked_exit_portals");

            var allowedEntryDirs = ParseDirectionList(metadata, "entry_dirs", "entry_directions");
            var deniedEntryDirs = ParseDirectionList(metadata, "deny_entry_dirs", "blocked_entry_dirs");
            var allowedExitDirs = ParseDirectionList(metadata, "exit_dirs", "exit_directions");
            var deniedExitDirs = ParseDirectionList(metadata, "deny_exit_dirs", "blocked_exit_dirs");

            if (GetBool(metadata, "one_way", "oneway"))
            {
                var dir = ParseDirectionList(metadata, "one_way_dir", "oneway_dir", "one_way_direction", "oneway_direction");
                if (dir.Count > 0 && allowedExitDirs.Count == 0)
                    allowedExitDirs = dir;
            }

            return new TrackSectorRules(
                sector.Id,
                isClosed,
                isRestricted,
                requiresStop,
                requiresYield,
                minSpeed,
                maxSpeed,
                allowedEntryPortals,
                deniedEntryPortals,
                allowedExitPortals,
                deniedExitPortals,
                allowedEntryDirs,
                deniedEntryDirs,
                allowedExitDirs,
                deniedExitDirs,
                sector.Metadata);
        }

        private static bool Allows(
            IReadOnlyCollection<string> allowedPortals,
            IReadOnlyCollection<string> deniedPortals,
            IReadOnlyCollection<MapDirection> allowedDirections,
            IReadOnlyCollection<MapDirection> deniedDirections,
            string? portalId,
            MapDirection? direction)
        {
            if (!string.IsNullOrWhiteSpace(portalId) && deniedPortals.Contains(portalId.Trim()))
                return false;
            if (allowedPortals.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(portalId) || !allowedPortals.Contains(portalId.Trim()))
                    return false;
            }

            if (direction.HasValue && deniedDirections.Contains(direction.Value))
                return false;
            if (allowedDirections.Count > 0 && (!direction.HasValue || !allowedDirections.Contains(direction.Value)))
                return false;

            return true;
        }

        private static bool GetBool(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return false;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (TryParseBool(raw, out var value))
                    return value;
            }
            return false;
        }

        private static float? GetFloat(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    return value;
            }
            return null;
        }

        private static IReadOnlyCollection<string> ParseIdList(
            IReadOnlyDictionary<string, string> metadata,
            params string[] keys)
        {
            var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (metadata == null || metadata.Count == 0)
                return results;

            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                foreach (var token in SplitTokens(raw))
                {
                    if (!string.IsNullOrWhiteSpace(token))
                        results.Add(token.Trim());
                }
            }

            return results;
        }

        private static IReadOnlyCollection<MapDirection> ParseDirectionList(
            IReadOnlyDictionary<string, string> metadata,
            params string[] keys)
        {
            var results = new HashSet<MapDirection>();
            if (metadata == null || metadata.Count == 0)
                return results;

            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var raw))
                    continue;
                foreach (var token in SplitTokens(raw))
                {
                    if (TryParseDirection(token, out var dir))
                        results.Add(dir);
                }
            }

            return results;
        }

        private static IEnumerable<string> SplitTokens(string raw)
        {
            return raw.Split(new[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParseDirection(string token, out MapDirection direction)
        {
            direction = MapDirection.North;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var trimmed = token.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "n":
                case "north":
                    direction = MapDirection.North;
                    return true;
                case "e":
                case "east":
                    direction = MapDirection.East;
                    return true;
                case "s":
                case "south":
                    direction = MapDirection.South;
                    return true;
                case "w":
                case "west":
                    direction = MapDirection.West;
                    return true;
            }

            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees))
            {
                direction = ToDirectionFromDegrees(degrees);
                return true;
            }

            return false;
        }

        private static MapDirection ToDirectionFromDegrees(float degrees)
        {
            var normalized = degrees % 360f;
            if (normalized < 0f)
                normalized += 360f;

            if (normalized >= 45f && normalized < 135f)
                return MapDirection.East;
            if (normalized >= 135f && normalized < 225f)
                return MapDirection.South;
            if (normalized >= 225f && normalized < 315f)
                return MapDirection.West;
            return MapDirection.North;
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            if (bool.TryParse(raw, out value))
                return true;
            if (raw == "1")
            {
                value = true;
                return true;
            }
            if (raw == "0")
            {
                value = false;
                return true;
            }
            return false;
        }

        private static float ToHeadingDegrees(MapDirection direction)
        {
            return direction switch
            {
                MapDirection.North => 0f,
                MapDirection.East => 90f,
                MapDirection.South => 180f,
                MapDirection.West => 270f,
                _ => 0f
            };
        }

        private static float? ResolveDelta(PortalDefinition portal, float headingDegrees)
        {
            switch (portal.Role)
            {
                case PortalRole.Entry:
                    return TryDelta(headingDegrees, portal.EntryHeadingDegrees);
                case PortalRole.Exit:
                    return TryDelta(headingDegrees, portal.ExitHeadingDegrees);
                case PortalRole.EntryExit:
                    var entryDelta = TryDelta(headingDegrees, portal.EntryHeadingDegrees);
                    var exitDelta = TryDelta(headingDegrees, portal.ExitHeadingDegrees);
                    if (entryDelta.HasValue && exitDelta.HasValue)
                        return Math.Min(entryDelta.Value, exitDelta.Value);
                    return entryDelta ?? exitDelta;
                default:
                    return null;
            }
        }

        private static float? TryDelta(float heading, float? target)
        {
            if (!target.HasValue)
                return null;
            var diff = Math.Abs(NormalizeDegrees(heading - target.Value));
            return diff > 180f ? 360f - diff : diff;
        }

        private static float NormalizeDegrees(float degrees)
        {
            var result = degrees % 360f;
            if (result < 0f)
                result += 360f;
            return result;
        }
    }
}
