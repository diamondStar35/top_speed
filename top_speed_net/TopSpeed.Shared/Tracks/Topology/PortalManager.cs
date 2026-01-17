using System;
using System.Collections.Generic;
using System.Linq;

namespace TopSpeed.Tracks.Topology
{
    public sealed class TrackPortalManager
    {
        private readonly Dictionary<string, PortalDefinition> _portalsById;
        private readonly Dictionary<string, List<PortalDefinition>> _portalsBySector;
        private readonly Dictionary<string, List<LinkDefinition>> _linksByPortal;

        public TrackPortalManager(IEnumerable<PortalDefinition> portals, IEnumerable<LinkDefinition> links)
        {
            _portalsById = new Dictionary<string, PortalDefinition>(StringComparer.OrdinalIgnoreCase);
            _portalsBySector = new Dictionary<string, List<PortalDefinition>>(StringComparer.OrdinalIgnoreCase);
            _linksByPortal = new Dictionary<string, List<LinkDefinition>>(StringComparer.OrdinalIgnoreCase);

            if (portals != null)
            {
                foreach (var portal in portals)
                    AddPortal(portal);
            }

            if (links != null)
            {
                foreach (var link in links)
                    AddLink(link);
            }
        }

        public IReadOnlyCollection<PortalDefinition> Portals => _portalsById.Values;

        public bool TryGetPortal(string id, out PortalDefinition portal)
        {
            portal = null!;
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return _portalsById.TryGetValue(id.Trim(), out portal!);
        }

        public IReadOnlyList<PortalDefinition> GetPortalsForSector(string sectorId)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
                return Array.Empty<PortalDefinition>();
            return _portalsBySector.TryGetValue(sectorId.Trim(), out var list)
                ? list
                : Array.Empty<PortalDefinition>();
        }

        public IReadOnlyList<PortalDefinition> GetLinkedPortals(string portalId)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return Array.Empty<PortalDefinition>();
            if (!_linksByPortal.TryGetValue(portalId.Trim(), out var links))
                return Array.Empty<PortalDefinition>();

            var results = new List<PortalDefinition>();
            foreach (var link in links)
            {
                if (string.Equals(link.FromPortalId, portalId, StringComparison.OrdinalIgnoreCase))
                {
                    if (TryGetPortal(link.ToPortalId, out var toPortal))
                        results.Add(toPortal);
                }
                else if (string.Equals(link.ToPortalId, portalId, StringComparison.OrdinalIgnoreCase) &&
                         link.Direction == LinkDirection.TwoWay)
                {
                    if (TryGetPortal(link.FromPortalId, out var fromPortal))
                        results.Add(fromPortal);
                }
            }

            return results;
        }

        public IReadOnlyList<string> GetConnectedSectorIds(string sectorId)
        {
            if (string.IsNullOrWhiteSpace(sectorId))
                return Array.Empty<string>();

            var portals = GetPortalsForSector(sectorId);
            if (portals.Count == 0)
                return Array.Empty<string>();

            var sectorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var portal in portals)
            {
                foreach (var linked in GetLinkedPortals(portal.Id))
                {
                    if (!string.IsNullOrWhiteSpace(linked.SectorId))
                        sectorIds.Add(linked.SectorId);
                }
            }

            return sectorIds.ToList();
        }

        private void AddPortal(PortalDefinition portal)
        {
            if (portal == null)
                throw new ArgumentNullException(nameof(portal));

            _portalsById[portal.Id] = portal;
            if (!_portalsBySector.TryGetValue(portal.SectorId, out var list))
            {
                list = new List<PortalDefinition>();
                _portalsBySector[portal.SectorId] = list;
            }
            list.Add(portal);
        }

        private void AddLink(LinkDefinition link)
        {
            if (link == null)
                throw new ArgumentNullException(nameof(link));

            AddLinkToPortal(link.FromPortalId, link);
            if (link.Direction == LinkDirection.TwoWay)
                AddLinkToPortal(link.ToPortalId, link);
        }

        private void AddLinkToPortal(string portalId, LinkDefinition link)
        {
            if (string.IsNullOrWhiteSpace(portalId))
                return;
            if (!_linksByPortal.TryGetValue(portalId.Trim(), out var list))
            {
                list = new List<LinkDefinition>();
                _linksByPortal[portalId.Trim()] = list;
            }
            list.Add(link);
        }
    }
}
