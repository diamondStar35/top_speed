using System;
using System.Collections.Concurrent;
using TopSpeed.Core;
using TopSpeed.Network;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private sealed partial class MultiplayerDispatch
        {
            private readonly Game _owner;
            private readonly ClientPktReg _reg;
            private readonly ConcurrentQueue<QueuedPacket> _queue;

            public MultiplayerDispatch(Game owner)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _reg = new ClientPktReg();
                _queue = new ConcurrentQueue<QueuedPacket>();
                RegisterHandlers();
            }

            public void Enqueue(MultiplayerSession session, IncomingPacket packet)
            {
                _queue.Enqueue(new QueuedPacket(session, packet));
            }

            public void Clear()
            {
                while (_queue.TryDequeue(out _))
                {
                }
            }

            public void Process()
            {
                while (_queue.TryDequeue(out var queued))
                {
                    if (!ReferenceEquals(_owner._session, queued.Session))
                        continue;

                    _reg.TryDispatch(queued.Packet);
                    if (!ReferenceEquals(_owner._session, queued.Session))
                        return;
                }
            }

            private void RegisterHandlers()
            {
                RegisterControl();
                RegisterRoom();
                RegisterRace();
                RegisterMedia();
                RegisterLive();
                RegisterVoice();
                RegisterChat();
            }

            private readonly struct QueuedPacket
            {
                public QueuedPacket(MultiplayerSession session, IncomingPacket packet)
                {
                    Session = session;
                    Packet = packet;
                }

                public MultiplayerSession Session { get; }
                public IncomingPacket Packet { get; }
            }
        }
    }
}
