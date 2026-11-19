using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles
{
    internal enum PowerCurveUnit
    {
        Kilowatts,
        Horsepower,
        MetricHorsepower
    }

    internal enum TorqueProfileKind
    {
        Default,
        HighRevNa,
        TurboBroad,
        DieselLowRev,
        Motorcycle,
        Muscle,
        Economy,
        SportTurbo,
        Supercharged,
        HeavyTruck
    }

    internal readonly struct TorqueProfileParams
    {
        public readonly float RiseExponent;
        public readonly float FallExponent;
        public readonly float IdleTorqueFactor;
        public readonly float RedlineTorqueFactor;

        public TorqueProfileParams(float riseExponent, float fallExponent, float idleTorqueFactor, float redlineTorqueFactor)
        {
            RiseExponent = riseExponent;
            FallExponent = fallExponent;
            IdleTorqueFactor = idleTorqueFactor;
            RedlineTorqueFactor = redlineTorqueFactor;
        }
    }

    internal sealed partial class TorqueCurve
    {
        private readonly float[] _rpm;
        private readonly float[] _torque;

        public TorqueCurve(float[] rpm, float[] torque)
        {
            if (rpm == null || torque == null)
                throw new ArgumentNullException();
            if (rpm.Length != torque.Length || rpm.Length < 2)
                throw new ArgumentException("Torque curve requires at least 2 points.");

            var pairs = new List<(float rpm, float torque)>(rpm.Length);
            for (var i = 0; i < rpm.Length; i++)
            {
                var r = rpm[i];
                var t = torque[i];
                if (r <= 0f)
                    continue;
                pairs.Add((r, t));
            }

            if (pairs.Count < 2)
                throw new ArgumentException("Torque curve requires valid RPM values.");

            pairs.Sort((a, b) => a.rpm.CompareTo(b.rpm));
            _rpm = new float[pairs.Count];
            _torque = new float[pairs.Count];
            for (var i = 0; i < pairs.Count; i++)
            {
                _rpm[i] = pairs[i].rpm;
                _torque[i] = pairs[i].torque;
            }
        }

        public float Evaluate(float rpm)
        {
            if (rpm <= _rpm[0])
                return _torque[0];
            if (rpm >= _rpm[_rpm.Length - 1])
                return _torque[_torque.Length - 1];

            var idx = Array.BinarySearch(_rpm, rpm);
            if (idx >= 0)
                return _torque[idx];
            idx = ~idx;
            var i0 = Math.Max(0, idx - 1);
            var i1 = Math.Min(_rpm.Length - 1, idx);
            if (i0 == i1)
                return _torque[i0];
            var t = (rpm - _rpm[i0]) / (_rpm[i1] - _rpm[i0]);
            return _torque[i0] + ((_torque[i1] - _torque[i0]) * t);
        }
    }
}

