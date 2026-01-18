using System;
using System.Collections.Generic;
using System.Globalization;
using TopSpeed.Tracks.Guidance;
using TopSpeed.Tracks.Sectors;

namespace TopSpeed.Tracks.Topology
{
    public enum TrackBranchRole
    {
        Undefined = 0,
        Main,
        Branch,
        Merge,
        Split,
        Intersection,
        Curve,
        Connector
    }

    public sealed class TrackBranchExitDefinition
    {
        public TrackBranchExitDefinition(string portalId, float? headingDegrees, string? name = null, IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                throw new ArgumentException("Portal id is required.", nameof(portalId));

            PortalId = portalId.Trim();
            HeadingDegrees = headingDegrees;
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string PortalId { get; }
        public float? HeadingDegrees { get; }
        public string? Name { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }

    public sealed class TrackBranchDefinition
    {
        public TrackBranchDefinition(
            string id,
            string sectorId,
            string? name,
            TrackBranchRole role,
            string? entryPortalId,
            float? entryHeadingDegrees,
            IReadOnlyList<TrackBranchExitDefinition> exits,
            float? widthMeters,
            float? lengthMeters,
            float? alignmentToleranceDegrees,
            IReadOnlyDictionary<string, string>? metadata)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Branch id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(sectorId))
                throw new ArgumentException("Sector id is required.", nameof(sectorId));

            Id = id.Trim();
            SectorId = sectorId.Trim();
            var trimmedName = name?.Trim();
            Name = string.IsNullOrWhiteSpace(trimmedName) ? null : trimmedName;
            Role = role;
            EntryPortalId = string.IsNullOrWhiteSpace(entryPortalId) ? null : entryPortalId!.Trim();
            EntryHeadingDegrees = entryHeadingDegrees;
            Exits = exits ?? Array.Empty<TrackBranchExitDefinition>();
            WidthMeters = widthMeters;
            LengthMeters = lengthMeters;
            AlignmentToleranceDegrees = alignmentToleranceDegrees;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Id { get; }
        public string SectorId { get; }
        public string? Name { get; }
        public TrackBranchRole Role { get; }
        public string? EntryPortalId { get; }
        public float? EntryHeadingDegrees { get; }
        public IReadOnlyList<TrackBranchExitDefinition> Exits { get; }
        public float? WidthMeters { get; }
        public float? LengthMeters { get; }
        public float? AlignmentToleranceDegrees { get; }
        public IReadOnlyDictionary<string, string> Metadata { get; }
    }

    public sealed class TrackBranchManager
    {
        private readonly Dictionary<string, TrackBranchDefinition> _branchesById;
        private readonly Dictionary<string, List<TrackBranchDefinition>> _branchesBySector;
        private readonly TrackPortalManager _portalManager;

        public TrackBranchManager(
            IEnumerable<TrackSectorDefinition> sectors,
            IEnumerable<TrackApproachDefinition> approaches,
            TrackPortalManager portalManager)
        {
            _portalManager = portalManager ?? throw new ArgumentNullException(nameof(portalManager));
            _branchesById = new Dictionary<string, TrackBranchDefinition>(StringComparer.OrdinalIgnoreCase);
            _branchesBySector = new Dictionary<string, List<TrackBranchDefinition>>(StringComparer.OrdinalIgnoreCase);

            if (sectors != null)
            {
                foreach (var sector in sectors)
                {
                    if (sector == null)
                        continue;
                    BuildFromSector(sector);
                }
            }

            if (approaches != null)
            {
                foreach (var approach in approaches)
                {
                    if (approach == null)
                        continue;
                    BuildFromApproach(approach);
                }
            }
        }

        public IReadOnlyCollection<TrackBranchDefinition> Branches => _branchesById.Values;

        public bool TryGetBranch(string id, out TrackBranchDefinition branch)
        {
            branch = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _branchesById.TryGetValue(id.Trim(), out branch!);
        }

        public IReadOnlyList<TrackBranchDefinition> GetBranchesForSector(string sectorId)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
                return Array.Empty<TrackBranchDefinition>();
            return _branchesBySector.TryGetValue(sectorId.Trim(), out var list)
                ? list
                : Array.Empty<TrackBranchDefinition>();
        }

        private void BuildFromSector(TrackSectorDefinition sector)
        {
            if (!HasBranchMetadata(sector))
                return;

            var metadata = sector.Metadata;
            var id = GetString(metadata, "branch_id", "branch", "id") ?? $"{sector.Id}_branch";
            if (_branchesById.ContainsKey(id))
                return;

            var role = ParseRole(GetString(metadata, "branch_role", "role")) ?? InferRole(sector.Type);
            var name = GetString(metadata, "branch_name", "name") ?? sector.Name;
            var entryPortal = GetString(metadata, "branch_entry", "entry_portal", "entry");
            var entryHeading = GetHeading(metadata, "branch_entry_heading", "entry_heading");
            var width = GetFloat(metadata, "branch_width", "width", "lane_width");
            var length = GetFloat(metadata, "branch_length", "length");
            var tolerance = GetFloat(metadata, "branch_tolerance", "tolerance", "alignment_tolerance");

            var portals = _portalManager.GetPortalsForSector(sector.Id);
            if (string.IsNullOrWhiteSpace(entryPortal))
                entryPortal = ResolvePortalId(portals, PortalRole.Entry);
            if (!entryHeading.HasValue && !string.IsNullOrWhiteSpace(entryPortal))
                entryHeading = ResolvePortalHeading(entryPortal!, PortalRole.Entry);

            var exits = BuildExitList(metadata, portals, entryPortal);

            var branch = new TrackBranchDefinition(
                id,
                sector.Id,
                name,
                role,
                entryPortal,
                entryHeading,
                exits,
                width,
                length,
                tolerance,
                metadata);

            AddBranch(branch);
        }

        private void BuildFromApproach(TrackApproachDefinition approach)
        {
            if (!HasBranchMetadata(approach))
                return;

            var metadata = approach.Metadata;
            var id = GetString(metadata, "branch_id", "branch", "id") ?? $"{approach.SectorId}_branch";
            var name = GetString(metadata, "branch_name", "name");
            var role = ParseRole(GetString(metadata, "branch_role", "role"));
            var entryPortal = GetString(metadata, "branch_entry", "entry_portal", "entry") ?? approach.EntryPortalId;
            var entryHeading = GetHeading(metadata, "branch_entry_heading", "entry_heading") ?? approach.EntryHeadingDegrees;
            var width = GetFloat(metadata, "branch_width", "width", "lane_width") ?? approach.WidthMeters;
            var length = GetFloat(metadata, "branch_length", "length") ?? approach.LengthMeters;
            var tolerance = GetFloat(metadata, "branch_tolerance", "tolerance", "alignment_tolerance") ?? approach.AlignmentToleranceDegrees;

            var portals = _portalManager.GetPortalsForSector(approach.SectorId);
            var exits = BuildExitList(metadata, portals, entryPortal);
            if (exits.Count == 0 && !string.IsNullOrWhiteSpace(approach.ExitPortalId))
            {
                exits = new List<TrackBranchExitDefinition>
                {
                    new TrackBranchExitDefinition(approach.ExitPortalId!, approach.ExitHeadingDegrees)
                };
            }

            if (_branchesById.TryGetValue(id, out var existing))
            {
                var merged = new TrackBranchDefinition(
                    id,
                    existing.SectorId,
                    name ?? existing.Name,
                    role == TrackBranchRole.Undefined ? existing.Role : role,
                    entryPortal ?? existing.EntryPortalId,
                    entryHeading ?? existing.EntryHeadingDegrees,
                    exits.Count > 0 ? exits : existing.Exits,
                    width ?? existing.WidthMeters,
                    length ?? existing.LengthMeters,
                    tolerance ?? existing.AlignmentToleranceDegrees,
                    existing.Metadata);

                ReplaceBranch(existing, merged);
                return;
            }

            var branch = new TrackBranchDefinition(
                id,
                approach.SectorId,
                name,
                role,
                entryPortal,
                entryHeading,
                exits,
                width,
                length,
                tolerance,
                metadata);

            AddBranch(branch);
        }

        private void AddBranch(TrackBranchDefinition branch)
        {
            _branchesById[branch.Id] = branch;
            if (!_branchesBySector.TryGetValue(branch.SectorId, out var list))
            {
                list = new List<TrackBranchDefinition>();
                _branchesBySector[branch.SectorId] = list;
            }
            list.Add(branch);
        }

        private void ReplaceBranch(TrackBranchDefinition existing, TrackBranchDefinition updated)
        {
            _branchesById[existing.Id] = updated;
            if (_branchesBySector.TryGetValue(existing.SectorId, out var list))
            {
                var index = list.FindIndex(b => string.Equals(b.Id, existing.Id, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                    list[index] = updated;
            }
        }

        private bool HasBranchMetadata(TrackSectorDefinition sector)
        {
            if (sector == null)
                return false;
            if (sector.Metadata == null || sector.Metadata.Count == 0)
                return sector.Type == TrackSectorType.Intersection ||
                       sector.Type == TrackSectorType.Merge ||
                       sector.Type == TrackSectorType.Split;

            return HasBranchMetadata(sector.Metadata);
        }

        private bool HasBranchMetadata(TrackApproachDefinition approach)
        {
            if (approach == null || approach.Metadata == null || approach.Metadata.Count == 0)
                return false;
            return HasBranchMetadata(approach.Metadata);
        }

        private static bool HasBranchMetadata(IReadOnlyDictionary<string, string> metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return false;

            foreach (var key in metadata.Keys)
            {
                if (key.StartsWith("branch", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return metadata.ContainsKey("entry_portal") ||
                   metadata.ContainsKey("exit_portal") ||
                   metadata.ContainsKey("exit_portals") ||
                   metadata.ContainsKey("entry") ||
                   metadata.ContainsKey("exit");
        }

        private List<TrackBranchExitDefinition> BuildExitList(
            IReadOnlyDictionary<string, string> metadata,
            IReadOnlyList<PortalDefinition> portals,
            string? entryPortalId)
        {
            var exits = new List<TrackBranchExitDefinition>();
            var rawExits = GetString(metadata, "branch_exits", "exit_portals", "exits", "exit_portal", "exit");
            if (!string.IsNullOrWhiteSpace(rawExits))
            {
                foreach (var token in SplitTokens(rawExits!))
                {
                    if (TryParseExitToken(token, out var portalId, out var heading))
                    {
                        exits.Add(new TrackBranchExitDefinition(portalId, heading));
                    }
                }
            }

            if (exits.Count > 0)
                return exits;

            foreach (var portal in portals)
            {
                if (portal == null)
                    continue;
                if (!string.IsNullOrWhiteSpace(entryPortalId) &&
                    string.Equals(portal.Id, entryPortalId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (portal.Role == PortalRole.Exit || portal.Role == PortalRole.EntryExit)
                {
                    exits.Add(new TrackBranchExitDefinition(portal.Id, portal.ExitHeadingDegrees));
                }
            }

            return exits;
        }

        private static bool TryParseExitToken(string token, out string portalId, out float? heading)
        {
            portalId = string.Empty;
            heading = null;
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var trimmed = token.Trim();
            var separator = trimmed.IndexOfAny(new[] { ':', '@' });
            if (separator > 0)
            {
                portalId = trimmed.Substring(0, separator).Trim();
                var headingRaw = trimmed.Substring(separator + 1).Trim();
                if (TryParseHeading(headingRaw, out var headingValue))
                    heading = headingValue;
            }
            else
            {
                portalId = trimmed;
            }

            return !string.IsNullOrWhiteSpace(portalId);
        }

        private static IEnumerable<string> SplitTokens(string raw)
        {
            return raw.Split(new[] { ',', '|', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static TrackBranchRole? ParseRole(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var trimmed = raw.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "main":
                case "primary":
                    return TrackBranchRole.Main;
                case "branch":
                    return TrackBranchRole.Branch;
                case "merge":
                    return TrackBranchRole.Merge;
                case "split":
                    return TrackBranchRole.Split;
                case "intersection":
                    return TrackBranchRole.Intersection;
                case "curve":
                    return TrackBranchRole.Curve;
                case "connector":
                    return TrackBranchRole.Connector;
            }

            if (Enum.TryParse(raw, true, out TrackBranchRole parsed))
                return parsed;
            return null;
        }

        private static TrackBranchRole InferRole(TrackSectorType type)
        {
            return type switch
            {
                TrackSectorType.Intersection => TrackBranchRole.Intersection,
                TrackSectorType.Merge => TrackBranchRole.Merge,
                TrackSectorType.Split => TrackBranchRole.Split,
                TrackSectorType.Curve => TrackBranchRole.Curve,
                _ => TrackBranchRole.Undefined
            };
        }

        private static string? GetString(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return null;
        }

        private static float? GetFloat(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var value))
                    continue;
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
            return null;
        }

        private static float? GetHeading(IReadOnlyDictionary<string, string> metadata, params string[] keys)
        {
            if (metadata == null || metadata.Count == 0)
                return null;
            foreach (var key in keys)
            {
                if (!metadata.TryGetValue(key, out var value))
                    continue;
                if (TryParseHeading(value, out var heading))
                    return heading;
            }
            return null;
        }

        private static bool TryParseHeading(string value, out float heading)
        {
            heading = 0f;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var trimmed = value.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "n":
                case "north":
                    heading = 0f;
                    return true;
                case "e":
                case "east":
                    heading = 90f;
                    return true;
                case "s":
                case "south":
                    heading = 180f;
                    return true;
                case "w":
                case "west":
                    heading = 270f;
                    return true;
            }
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out heading);
        }

        private static string? ResolvePortalId(IReadOnlyList<PortalDefinition> portals, PortalRole role)
        {
            if (portals == null || portals.Count == 0)
                return null;

            foreach (var portal in portals)
            {
                if (portal.Role == role || portal.Role == PortalRole.EntryExit)
                    return portal.Id;
            }
            return portals[0].Id;
        }

        private float? ResolvePortalHeading(string portalId, PortalRole role)
        {
            if (!_portalManager.TryGetPortal(portalId, out var portal))
                return null;

            return role switch
            {
                PortalRole.Entry => portal.EntryHeadingDegrees ?? portal.ExitHeadingDegrees,
                PortalRole.Exit => portal.ExitHeadingDegrees ?? portal.EntryHeadingDegrees,
                _ => portal.EntryHeadingDegrees ?? portal.ExitHeadingDegrees
            };
        }
    }
}
