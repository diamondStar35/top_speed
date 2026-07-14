using System;
using System.Numerics;
using TopSpeed.Audio;
using TopSpeed.Vehicles;

namespace TopSpeed.Drive.Session.Systems
{
    internal sealed class Listener : Subsystem
    {
        private readonly AudioManager _audio;
        private readonly ICar _car;
        private readonly VehicleRadioController _localRadio;
        private readonly Func<float?>? _getListenerXOverride;
        private Vector3 _lastListenerPosition;
        private bool _listenerInitialized;

        public Listener(
            string name,
            int order,
            AudioManager audio,
            ICar car,
            VehicleRadioController localRadio,
            Func<float?>? getListenerXOverride = null)
            : base(name, order)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _car = car ?? throw new ArgumentNullException(nameof(car));
            _localRadio = localRadio ?? throw new ArgumentNullException(nameof(localRadio));
            _getListenerXOverride = getListenerXOverride;
        }

        public override void Update(SessionContext context, float elapsed)
        {
            var driverOffsetX = -_car.WidthM * 0.25f;
            var driverOffsetZ = _car.LengthM * 0.1f;
            var overrideX = _getListenerXOverride?.Invoke();
            var rawX = overrideX.HasValue ? overrideX.Value : _car.PositionX;
            var worldPosition = new Vector3(rawX + driverOffsetX, 0f, _car.PositionY + driverOffsetZ);

            var worldVelocity = Vector3.Zero;
            if (_listenerInitialized && elapsed > 0f)
                worldVelocity = (worldPosition - _lastListenerPosition) / elapsed;

            _lastListenerPosition = worldPosition;
            _listenerInitialized = true;

            var forward = new Vector3(0f, 0f, 1f);
            var up = new Vector3(0f, 1f, 0f);
            _audio.UpdateListener(AudioWorld.ToMeters(worldPosition), forward, up, AudioWorld.ToMeters(worldVelocity));
            _localRadio.UpdateSpatial(worldPosition.X, worldPosition.Z, worldVelocity);
        }

        public void Reset()
        {
            _lastListenerPosition = Vector3.Zero;
            _listenerInitialized = false;
        }
    }
}

