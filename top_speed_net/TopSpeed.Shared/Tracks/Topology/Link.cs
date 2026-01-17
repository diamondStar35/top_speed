using System;

namespace TopSpeed.Tracks.Topology
{
    public sealed class LinkDefinition
    {
        public LinkDefinition(string id, string fromPortalId, string toPortalId, LinkDirection direction)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Link id is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(fromPortalId))
                throw new ArgumentException("From portal id is required.", nameof(fromPortalId));
            if (string.IsNullOrWhiteSpace(toPortalId))
                throw new ArgumentException("To portal id is required.", nameof(toPortalId));

            Id = id.Trim();
            FromPortalId = fromPortalId.Trim();
            ToPortalId = toPortalId.Trim();
            Direction = direction;
        }

        public string Id { get; }
        public string FromPortalId { get; }
        public string ToPortalId { get; }
        public LinkDirection Direction { get; }
    }
}
