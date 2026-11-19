using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Multiplayer
{
    internal sealed class RemotePlayer
    {
        public RemotePlayer(ComputerPlayer player)
        {
            Player = player;
            State = PlayerState.NotReady;
        }

        public ComputerPlayer Player { get; }
        public PlayerState State { get; set; }
        public bool Finished { get; set; }
    }
}
