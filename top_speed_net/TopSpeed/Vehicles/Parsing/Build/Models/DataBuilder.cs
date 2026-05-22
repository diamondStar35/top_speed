using System.IO;
using System.Linq;
using TopSpeed.Physics.Tires.Wear;

namespace TopSpeed.Vehicles.Parsing
{
    internal static partial class VehicleTsvParser
    {
        private static CustomVehicleTsvData BuildParsedData(
            string fullPath,
            ParsedValues values,
            string? torqueCurvePreset,
            float[] torqueCurveRpm,
            float[] torqueCurveTorqueNm,
            float tireCircumferenceResolved,
            TransmissionPolicy transmissionPolicy)
        {
            return new CustomVehicleTsvData
            {
                SourcePath = fullPath,
                SourceDirectory = Path.GetDirectoryName(fullPath) ?? string.Empty,
                Meta = new CustomVehicleMeta(values.MetaName, values.MetaVersion, values.MetaDescription),
                Sounds = new CustomVehicleSounds
                {
                    Engine = values.EngineSound,
                    Start = values.StartSound,
                    Stop = values.StopSound,
                    Horn = values.HornSound,
                    Throttle = values.ThrottleSound,
                    CrashVariants = values.CrashVariants,
                    Brake = values.BrakeSound,
                    BackfireVariants = values.BackfireVariants
                },
                SurfaceTractionFactor = values.SurfaceTractionFactor,
                TopSpeed = values.TopSpeed,
                HasWipers = values.HasWipers ? 1 : 0,
                IdleFreq = values.IdleFreq,
                TopFreq = values.TopFreq,
                ShiftFreq = values.ShiftFreq,
                PitchCurveExponent = TopSpeed.Vehicles.VehicleDefinition.ClampPitchCurveExponent(values.PitchCurveExponent),
                Gears = values.GearCount,
                GearRatios = values.GearRatios!.ToArray(),
                PrimaryTransmissionType = values.PrimaryTransmissionType,
                SupportedTransmissionTypes = values.SupportedTransmissionTypes.ToArray(),
                ShiftOnDemand = values.ShiftOnDemand,
                AutomaticTuning = values.AutomaticTuning,
                IdleRpm = values.IdleRpm,
                MaxRpm = values.MaxRpm,
                RevLimiter = values.RevLimiter,
                AutoShiftRpm = values.AutoShiftRpm,
                EngineBraking = values.EngineBraking,
                FuelTankCapacityLiters = values.FuelTankCapacityLiters,
                EngineDisplacementLiters = values.EngineDisplacementLiters,
                MassKg = values.MassKg,
                DrivetrainEfficiency = values.DrivetrainEfficiency,
                EngineBrakingTorqueNm = values.EngineBrakingTorque,
                PeakTorqueNm = values.PeakTorque,
                PeakTorqueRpm = values.PeakTorqueRpm,
                IdleTorqueNm = values.IdleTorque,
                RedlineTorqueNm = values.RedlineTorque,
                DragCoefficient = values.DragCoefficient,
                FrontalAreaM2 = values.FrontalArea,
                SideAreaM2 = values.SideArea,
                RollingResistanceCoefficient = values.RollingResistance,
                WheelSideDragBaseN = values.WheelSideDragBaseN,
                WheelSideDragLinearNPerMps = values.WheelSideDragLinearNPerMps,
                RollingResistanceSpeedFactor = values.RollingResistanceSpeedFactor,
                LaunchRpm = values.LaunchRpm,
                CoupledDrivelineDragNm = values.CoupledDrivelineDragNm,
                CoupledDrivelineViscousDragNmPerKrpm = values.CoupledDrivelineViscousDragNmPerKrpm,
                EngineInertiaKgm2 = values.EngineInertiaKgm2,
                EngineFrictionTorqueNm = values.EngineFrictionBaseNm,
                EngineFrictionLinearNmPerKrpm = values.EngineFrictionLinearNmPerKrpm,
                EngineFrictionQuadraticNmPerKrpm2 = values.EngineFrictionQuadraticNmPerKrpm2,
                DrivelineCouplingRate = values.DrivelineCouplingRate,
                IdleControlWindowRpm = values.IdleControlWindowRpm,
                IdleControlGainNmPerRpm = values.IdleControlGainNmPerRpm,
                MinCoupledRiseIdleRpmPerSecond = values.MinCoupledRiseIdleRpmPerSecond,
                MinCoupledRiseFullRpmPerSecond = values.MinCoupledRiseFullRpmPerSecond,
                EngineOverrunIdleLossFraction = values.EngineOverrunIdleLossFraction,
                OverrunCurveExponent = values.OverrunCurveExponent,
                EngineBrakeTransferEfficiency = values.EngineBrakeTransferEfficiency,
                PowerFactor = values.PowerFactor,
                TorqueCurvePreset = torqueCurvePreset,
                TorqueCurveRpm = torqueCurveRpm,
                TorqueCurveTorqueNm = torqueCurveTorqueNm,
                FinalDriveRatio = values.FinalDrive,
                ReverseMaxSpeedKph = values.ReverseMaxSpeed,
                ReversePowerFactor = values.ReversePowerFactor,
                ReverseGearRatio = values.ReverseGearRatio,
                BrakeStrength = values.BrakeStrength,
                Steering = values.SteeringResponse,
                TireGripCoefficient = values.TireGrip,
                TireWearConfig = ResolveTireWearConfig(values, tireCircumferenceResolved),
                LateralGripCoefficient = values.LateralGrip,
                HighSpeedStability = values.HighSpeedStability,
                WheelbaseM = values.Wheelbase,
                MaxSteerDeg = values.MaxSteerDeg,
                HighSpeedSteerGain = values.HighSpeedSteerGain,
                HighSpeedSteerStartKph = values.HighSpeedSteerStartKph,
                HighSpeedSteerFullKph = values.HighSpeedSteerFullKph,
                CombinedGripPenalty = values.CombinedGripPenalty,
                SlipAnglePeakDeg = values.SlipAnglePeakDeg,
                SlipAngleFalloff = values.SlipAngleFalloff,
                TurnResponse = values.TurnResponse,
                MassSensitivity = values.MassSensitivity,
                DownforceGripGain = values.DownforceGripGain,
                CornerStiffnessFront = values.CornerStiffnessFront,
                CornerStiffnessRear = values.CornerStiffnessRear,
                YawInertiaScale = values.YawInertiaScale,
                SteeringCurve = values.SteeringCurve,
                TransientDamping = values.TransientDamping,
                WidthM = values.WidthM,
                LengthM = values.LengthM,
                TireCircumferenceM = tireCircumferenceResolved,
                TransmissionPolicy = transmissionPolicy
            };
        }

        private static TireWearConfig ResolveTireWearConfig(ParsedValues values, float tireCircumferenceResolved)
        {
            var profile = TireWearProfiles.CreateFromVehicle(
                values.TireGrip,
                values.MassKg,
                tireCircumferenceResolved,
                values.LateralGrip);

            return new TireWearConfig
            {
                BaseWearPerKilometer = values.TireWearBasePerKilometer ?? profile.BaseWearPerKilometer,
                SlipWearRatePerSecond = values.TireWearSlipRatePerSecond ?? profile.SlipWearRatePerSecond,
                CorneringSlipWearWeight = values.TireWearCorneringSlipWeight ?? profile.CorneringSlipWearWeight,
                LongitudinalSlipWearWeight = values.TireWearLongitudinalSlipWeight ?? profile.LongitudinalSlipWearWeight,
                LoadWearGain = values.TireWearLoadGain ?? profile.LoadWearGain,
                WearHotStartTemperatureC = values.TireWearHotStartTemperatureC ?? profile.WearHotStartTemperatureC,
                WearHotGainPerC = values.TireWearHotGainPerC ?? profile.WearHotGainPerC,
                WearColdStartTemperatureC = values.TireWearColdStartTemperatureC ?? profile.WearColdStartTemperatureC,
                WearColdGainPerC = values.TireWearColdGainPerC ?? profile.WearColdGainPerC,
                ColdEndTemperatureC = values.TireTemperatureColdEndC ?? profile.ColdEndTemperatureC,
                OptimalStartTemperatureC = values.TireTemperatureOptimalStartC ?? profile.OptimalStartTemperatureC,
                OptimalEndTemperatureC = values.TireTemperatureOptimalEndC ?? profile.OptimalEndTemperatureC,
                OverheatEndTemperatureC = values.TireTemperatureOverheatEndC ?? profile.OverheatEndTemperatureC,
                GripAtVeryCold = values.TireGripVeryCold ?? profile.GripAtVeryCold,
                GripAtColdEnd = values.TireGripColdEnd ?? profile.GripAtColdEnd,
                GripAtOptimal = values.TireGripOptimal ?? profile.GripAtOptimal,
                GripAtOverheatEnd = values.TireGripOverheatEnd ?? profile.GripAtOverheatEnd,
                GripAtCooked = values.TireGripCooked ?? profile.GripAtCooked,
                GripAtFullWear = values.TireWearGripAtFullWear ?? profile.GripAtFullWear,
                CorneringHeatCPerSecond = values.TireHeatCorneringCPerSecond ?? profile.CorneringHeatCPerSecond,
                LongitudinalHeatCPerSecond = values.TireHeatLongitudinalCPerSecond ?? profile.LongitudinalHeatCPerSecond,
                LoadHeatCPerSecond = values.TireHeatLoadCPerSecond ?? profile.LoadHeatCPerSecond,
                RollingHeatCPerSecond = values.TireHeatRollingCPerSecond ?? profile.RollingHeatCPerSecond,
                AirflowCoolingPerMpsPerCPerSecond = values.TireCoolingAirflowPerMpsPerCPerSecond ?? profile.AirflowCoolingPerMpsPerCPerSecond,
                AmbientExchangePerCPerSecond = values.TireExchangeAmbientPerCPerSecond ?? profile.AmbientExchangePerCPerSecond,
                RoadExchangePerCPerSecond = values.TireExchangeRoadPerCPerSecond ?? profile.RoadExchangePerCPerSecond,
                WetRoadExchangePerCPerSecond = values.TireExchangeWetRoadPerCPerSecond ?? profile.WetRoadExchangePerCPerSecond,
                InternalConductancePerSecond = values.TireInternalConductancePerSecond ?? profile.InternalConductancePerSecond,
                CarcassMassRatio = values.TireCarcassMassRatio ?? profile.CarcassMassRatio,
                SlipSmoothingTimeConstantSeconds = values.TireSlipSmoothingTauSeconds ?? profile.SlipSmoothingTimeConstantSeconds,
            };
        }
    }
}

