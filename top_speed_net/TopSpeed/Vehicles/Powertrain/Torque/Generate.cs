using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles
{
    internal sealed partial class TorqueCurve
    {
        public static TorqueCurve Generate(
            float idleRpm,
            float maxRpm,
            float revLimiter,
            float peakTorqueRpm,
            float idleTorqueNm,
            float peakTorqueNm,
            float redlineTorqueNm,
            float massKg,
            float frontalAreaM2,
            TorqueProfileKind? profileOverride = null)
        {
            var idle = Math.Max(500f, idleRpm);
            var rev = Math.Max(idle + 1000f, Math.Max(maxRpm, revLimiter));
            var peakRpm = peakTorqueRpm > 0f
                ? peakTorqueRpm
                : idle + ((rev - idle) * 0.60f);
            peakRpm = Clamp(peakRpm, idle + 200f, rev - 200f);

            var peakTorque = Math.Max(peakTorqueNm, Math.Max(idleTorqueNm, redlineTorqueNm));
            if (peakTorque <= 0f)
                peakTorque = 1f;

            var profile = profileOverride ?? SelectProfile(maxRpm, peakRpm, massKg, frontalAreaM2);
            var profileParams = GetProfileParams(profile);

            if (idleTorqueNm <= 0f)
                idleTorqueNm = peakTorque * profileParams.IdleTorqueFactor;
            if (redlineTorqueNm <= 0f)
                redlineTorqueNm = peakTorque * profileParams.RedlineTorqueFactor;

            idleTorqueNm = Math.Max(0f, Math.Min(idleTorqueNm, peakTorque * 0.98f));
            redlineTorqueNm = Math.Max(0f, Math.Min(redlineTorqueNm, peakTorque * 0.98f));

            var xPeak = (peakRpm - idle) / (rev - idle);
            xPeak = Clamp(xPeak, 0.10f, 0.95f);

            var samples = new List<float>(16);
            AddSample(samples, 0f);
            AddSample(samples, 0.05f);
            AddSample(samples, 0.10f);
            AddSample(samples, 0.15f);
            AddSample(samples, 0.20f);
            AddSample(samples, 0.30f);
            AddSample(samples, 0.40f);
            AddSample(samples, 0.50f);
            AddSample(samples, 0.60f);
            AddSample(samples, 0.70f);
            AddSample(samples, 0.80f);
            AddSample(samples, 0.90f);
            AddSample(samples, 0.95f);
            AddSample(samples, 1f);
            AddSample(samples, xPeak - 0.03f);
            AddSample(samples, xPeak);
            AddSample(samples, xPeak + 0.03f);
            samples.Sort();

            var rpm = new float[samples.Count];
            var torque = new float[samples.Count];
            for (var i = 0; i < samples.Count; i++)
            {
                var x = samples[i];
                rpm[i] = idle + (x * (rev - idle));
                torque[i] = EvaluateGeneratedTorque(
                    x,
                    xPeak,
                    idleTorqueNm,
                    peakTorque,
                    redlineTorqueNm,
                    profileParams.RiseExponent,
                    profileParams.FallExponent);
            }

            return new TorqueCurve(rpm, torque);
        }

        private static float EvaluateGeneratedTorque(
            float x,
            float xPeak,
            float idleTorqueNm,
            float peakTorqueNm,
            float redlineTorqueNm,
            float riseExponent,
            float fallExponent)
        {
            if (x <= xPeak)
            {
                var t = xPeak <= 0f ? 0f : x / xPeak;
                t = (float)Math.Pow(Clamp(t, 0f, 1f), riseExponent);
                return Lerp(idleTorqueNm, peakTorqueNm, t);
            }

            var denom = 1f - xPeak;
            var tFall = denom <= 0f ? 1f : (x - xPeak) / denom;
            tFall = (float)Math.Pow(Clamp(tFall, 0f, 1f), fallExponent);
            return Lerp(peakTorqueNm, redlineTorqueNm, tFall);
        }

        private static void AddSample(List<float> samples, float value)
        {
            if (value < 0f || value > 1f)
                return;
            const float epsilon = 0.004f;
            for (var i = 0; i < samples.Count; i++)
            {
                if (Math.Abs(samples[i] - value) <= epsilon)
                    return;
            }

            samples.Add(value);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}

