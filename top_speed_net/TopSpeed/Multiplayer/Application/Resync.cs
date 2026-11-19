namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void RequestMultiplayerRoomResync()
        {
            _session?.SendRoomStateRequest();
        }
    }
}
