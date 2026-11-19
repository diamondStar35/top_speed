using System;
using System.Collections.Generic;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core
{
    internal sealed class ClientPktReg
    {
        private readonly Dictionary<Command, Entry> _map = new Dictionary<Command, Entry>();

        internal delegate bool H(IncomingPacket packet);

        private readonly struct Entry
        {
            public Entry(string module, H handler)
            {
                Module = module;
                Handler = handler;
            }

            public string Module { get; }
            public H Handler { get; }
        }

        public void Add(string module, Command command, H handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_map.ContainsKey(command))
                throw new InvalidOperationException($"Client packet handler already registered for {command}.");

            _map[command] = new Entry(module ?? string.Empty, handler);
        }

        public bool TryDispatch(IncomingPacket packet)
        {
            if (!_map.TryGetValue(packet.Command, out var entry))
                return false;

            return entry.Handler(packet);
        }
    }
}

