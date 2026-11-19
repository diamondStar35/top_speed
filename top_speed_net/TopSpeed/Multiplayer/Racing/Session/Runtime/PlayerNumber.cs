namespace TopSpeed.Drive.Multiplayer
{
    internal sealed partial class MultiplayerSession
    {
        private void HandleLocalPlayerNumberRequest()
        {
            if (!_input.GetPlayerNumber())
                return;

            var index = LocalPlayerNumber + 1;
            QueueSound(GetNumberSound(index));
        }
    }
}
