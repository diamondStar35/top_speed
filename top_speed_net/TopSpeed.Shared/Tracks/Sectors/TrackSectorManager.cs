using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TopSpeed.Tracks.Areas;
using TopSpeed.Tracks.Topology;

namespace TopSpeed.Tracks.Sectors
{
    public sealed class TrackSectorManager
    {
        private readonly Dictionary<string, TrackSectorDefinition> _sectorsById;
        private readonly TrackAreaManager _areaManager;
        private readonly TrackPortalManager _portalManager;

        public TrackSectorManager(
            IEnumerable<TrackSectorDefinition> sectors,
            TrackAreaManager areaManager,
            TrackPortalManager portalManager)
        {
            if (areaManager == null)
                throw new ArgumentNullException(nameof(areaManager));
            if (portalManager == null)
                throw new ArgumentNullException(nameof(portalManager));

            _areaManager = areaManager;
            _portalManager = portalManager;
            _sectorsById = new Dictionary<string, TrackSectorDefinition>(StringComparer.OrdinalIgnoreCase);

            if (sectors != null)
            {
                foreach (var sector in sectors)
                    AddSector(sector);
            }
        }

        public IReadOnlyCollection<TrackSectorDefinition> Sectors => _sectorsById.Values;

        public bool TryGetSector(string id, out TrackSectorDefinition sector)
        {
            sector = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _sectorsById.TryGetValue(id.Trim(), out sector!);
        }

        public IReadOnlyList<TrackSectorDefinition> FindSectorsContaining(Vector2 position)
        {
            if (_sectorsById.Count == 0)
                return Array.Empty<TrackSectorDefinition>();

            var hits = new List<TrackSectorDefinition>();
            foreach (var sector in _sectorsById.Values)
            {
                if (Contains(sector, position))
                    hits.Add(sector);
            }
            return hits;
        }

        public bool TryLocate(
            Vector2 position,
            float? headingDegrees,
            out TrackSectorDefinition sector,
            out PortalDefinition? portal,
            out float? portalDeltaDegrees)
        {
            sector = null!;
            portal = null;
            portalDeltaDegrees = null;

            var candidates = FindSectorsContaining(position);
            if (candidates.Count == 0)
                return false;

            if (!headingDegrees.HasValue || candidates.Count == 1)
            {
                sector = candidates[0];
                portal = FindBestPortal(sector.Id, headingDegrees, out portalDeltaDegrees);
                return true;
            }

            var bestScore = float.MaxValue;
            TrackSectorDefinition? bestSector = null;
            PortalDefinition? bestPortal = null;
            float? bestDelta = null;

            foreach (var candidate in candidates)
            {
                var candidatePortal = FindBestPortal(candidate.Id, headingDegrees, out var delta);
                var score = delta ?? 180f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestSector = candidate;
                    bestPortal = candidatePortal;
                    bestDelta = delta;
                }
            }

            if (bestSector == null)
                return false;

            sector = bestSector;
            portal = bestPortal;
            portalDeltaDegrees = bestDelta;
            return true;
        }

        public IReadOnlyList<PortalDefinition> GetPortalsForSector(string sectorId)
        {
            return _portalManager.GetPortalsForSector(sectorId);
        }

        public IReadOnlyList<string> GetConnectedSectorIds(string sectorId)
        {
            return _portalManager.GetConnectedSectorIds(sectorId);
        }

        private void AddSector(TrackSectorDefinition sector)
        {
            if (sector == null)
                throw new ArgumentNullException(nameof(sector));
            _sectorsById[sector.Id] = sector;
        }

        private bool Contains(TrackSectorDefinition sector, Vector2 position)
        {
            if (sector == null || string.IsNullOrWhiteSpace(sector.AreaId))
                return false;
            var areaId = sector.AreaId!.Trim();
            if (_areaManager.TryGetArea(areaId, out var area))
                return _areaManager.Contains(area, position);
            return _areaManager.ContainsShape(areaId, position);
        }

        private PortalDefinition? FindBestPortal(string sectorId, float? headingDegrees, out float? deltaDegrees)
        {
            deltaDegrees = null;
            if (!headingDegrees.HasValue)
                return null;

            var portals = _portalManager.GetPortalsForSector(sectorId);
            if (portals.Count == 0)
                return null;

            var heading = NormalizeDegrees(headingDegrees.Value);
            PortalDefinition? bestPortal = null;
            var bestDelta = float.MaxValue;

            foreach (var portal in portals)
            {
                if (!TryGetPortalHeading(portal, heading, out var delta))
                    continue;
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestPortal = portal;
                }
            }

            if (bestPortal == null)
                return null;

            deltaDegrees = bestDelta;
            return bestPortal;
        }

        private static bool TryGetPortalHeading(PortalDefinition portal, float heading, out float deltaDegrees)
        {
            deltaDegrees = 0f;
            if (portal == null)
                return false;

            switch (portal.Role)
            {
                case PortalRole.Entry:
                    return TryDelta(heading, portal.EntryHeadingDegrees, out deltaDegrees);
                case PortalRole.Exit:
                    return TryDelta(heading, portal.ExitHeadingDegrees, out deltaDegrees);
                case PortalRole.EntryExit:
                    var hasEntry = TryDelta(heading, portal.EntryHeadingDegrees, out var entryDelta);
                    var hasExit = TryDelta(heading, portal.ExitHeadingDegrees, out var exitDelta);
                    if (hasEntry && hasExit)
                    {
                        deltaDegrees = Math.Min(entryDelta, exitDelta);
                        return true;
                    }
                    if (hasEntry)
                    {
                        deltaDegrees = entryDelta;
                        return true;
                    }
                    if (hasExit)
                    {
                        deltaDegrees = exitDelta;
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static bool TryDelta(float heading, float? target, out float deltaDegrees)
        {
            deltaDegrees = 0f;
            if (!target.HasValue)
                return false;
            var diff = Math.Abs(NormalizeDegrees(heading - target.Value));
            deltaDegrees = diff > 180f ? 360f - diff : diff;
            return true;
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
