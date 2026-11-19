using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Vehicles.Parsing;

namespace TopSpeed.Vehicles.Loader
{
    internal static class Sound
    {
        private const string BuiltinPrefix = "builtin";
        private const string DefaultVehicleFolder = "default";

        public static string? ResolveOfficialFallback(string root, string vehicleFolder, VehicleAction action)
        {
            var fileName = GetDefaultFileName(action);
            var primaryPath = Path.GetFullPath(Path.Combine(root, vehicleFolder, fileName));
            if (File.Exists(primaryPath))
                return primaryPath;

            if (action == VehicleAction.Backfire || action == VehicleAction.Throttle || action == VehicleAction.Stop)
                return null;

            var fallbackPath = Path.GetFullPath(Path.Combine(root, DefaultVehicleFolder, fileName));
            if (File.Exists(fallbackPath))
                return fallbackPath;

            return null;
        }

        public static string[] ResolveCustomList(
            IReadOnlyList<string> values,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction)
        {
            var result = new List<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var resolved = ResolveCustom(values[i], builtinRoot, vehicleRoot, builtinAction);
                if (!string.IsNullOrWhiteSpace(resolved))
                    result.Add(resolved!);
            }

            if (result.Count == 0)
                throw new InvalidDataException($"No valid sound paths resolved for {builtinAction}.");

            return result.ToArray();
        }

        public static string ResolveCustom(
            string value,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction)
        {
            if (!TryResolveCustom(value, builtinRoot, vehicleRoot, builtinAction, out var resolved, out var error, out var missingFile))
            {
                if (missingFile != null)
                    throw new FileNotFoundException(error, missingFile);
                throw new InvalidDataException(error);
            }

            return resolved!;
        }

        /// <summary>
        /// Non-throwing counterpart to <see cref="ResolveCustom"/>. Returns false with a human-readable
        /// <paramref name="error"/> instead of throwing so callers (the custom-vehicles menu, the race
        /// loader's graceful fallback) can surface problems through the warning system rather than crashing.
        /// When the failure is specifically a missing file on disk, <paramref name="missingFile"/> holds the
        /// resolved path; when it is a malformed/unsafe reference, <paramref name="missingFile"/> is null.
        /// </summary>
        public static bool TryResolveCustom(
            string value,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction,
            out string? resolved,
            out string? error,
            out string? missingFile)
        {
            resolved = null;
            error = null;
            missingFile = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                error = $"Missing required sound value for {builtinAction}.";
                return false;
            }

            var trimmed = value.Trim();
            if (trimmed.StartsWith(BuiltinPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fromBuiltin = ResolveCustomBuiltin(trimmed, builtinRoot, builtinAction);
                if (!string.IsNullOrWhiteSpace(fromBuiltin))
                {
                    resolved = fromBuiltin!;
                    return true;
                }

                error = $"Builtin sound reference '{trimmed}' for {builtinAction} could not be resolved.";
                return false;
            }

            if (Path.IsPathRooted(trimmed))
            {
                error = $"Absolute sound paths are not allowed for custom vehicles: {trimmed}";
                return false;
            }

            var normalized = trimmed
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            if (normalized.IndexOf(':') >= 0 || ContainsTraversal(normalized))
            {
                error = $"Invalid custom sound path '{trimmed}'. Paths must stay inside the vehicle folder.";
                return false;
            }

            var rootFull = Path.GetFullPath(vehicleRoot);
            var candidate = Path.GetFullPath(Path.Combine(rootFull, normalized));
            if (!IsInsideRoot(rootFull, candidate))
            {
                error = $"Custom sound path '{trimmed}' escapes the vehicle folder.";
                return false;
            }

            if (!File.Exists(candidate))
            {
                missingFile = candidate;
                error = $"Custom vehicle sound file not found: {candidate}";
                return false;
            }

            resolved = candidate;
            return true;
        }

        /// <summary>
        /// One problem found while validating a custom vehicle's declared sounds. <see cref="Required"/>
        /// distinguishes a slot the car cannot run without (engine, start, horn, brake, at least one crash)
        /// from an optional one (stop, throttle, backfire) that can simply be skipped.
        /// </summary>
        public readonly struct SoundIssue
        {
            public SoundIssue(VehicleAction action, bool required, string message, string? missingPath = null)
            {
                Action = action;
                Required = required;
                Message = message;
                MissingPath = missingPath;
            }

            public VehicleAction Action { get; }
            public bool Required { get; }
            public string Message { get; }

            // Set when the failure is specifically a missing file on disk; null for malformed/unsafe
            // references. Lets the menu render "sound file not found: <path>" without re-deriving it.
            public string? MissingPath { get; }
        }

        /// <summary>
        /// Walks every declared sound reference for a parsed custom vehicle and reports the ones that do not
        /// resolve (missing files, unsafe paths, unresolvable builtins). Does not throw and does not stop at
        /// the first problem, so the caller can show a complete picture. Builtin references and absent optional
        /// slots that are legitimately empty produce no issue.
        /// </summary>
        public static IReadOnlyList<SoundIssue> ValidateCustomSounds(
            CustomVehicleSounds sounds,
            string builtinRoot,
            string vehicleRoot)
        {
            var issues = new List<SoundIssue>();

            CheckRequired(issues, sounds.Engine, builtinRoot, vehicleRoot, VehicleAction.Engine);
            CheckRequired(issues, sounds.Start, builtinRoot, vehicleRoot, VehicleAction.Start);
            CheckRequired(issues, sounds.Horn, builtinRoot, vehicleRoot, VehicleAction.Horn);
            CheckRequired(issues, sounds.Brake, builtinRoot, vehicleRoot, VehicleAction.Brake);
            CheckRequiredList(issues, sounds.CrashVariants, builtinRoot, vehicleRoot, VehicleAction.Crash);

            CheckOptional(issues, sounds.Stop, builtinRoot, vehicleRoot, VehicleAction.Stop);
            CheckOptional(issues, sounds.Throttle, builtinRoot, vehicleRoot, VehicleAction.Throttle);
            CheckOptionalList(issues, sounds.BackfireVariants, builtinRoot, vehicleRoot, VehicleAction.Backfire);

            return issues;
        }

        private static void CheckRequired(
            List<SoundIssue> issues, string value, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            if (!TryResolveCustom(value, builtinRoot, vehicleRoot, action, out _, out var error, out var missing))
                issues.Add(new SoundIssue(action, true, error!, missing));
        }

        private static void CheckOptional(
            List<SoundIssue> issues, string? value, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            if (!TryResolveCustom(value!, builtinRoot, vehicleRoot, action, out _, out var error, out var missing))
                issues.Add(new SoundIssue(action, false, error!, missing));
        }

        private static void CheckRequiredList(
            List<SoundIssue> issues, IReadOnlyList<string> values, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            if (values == null || values.Count == 0)
            {
                issues.Add(new SoundIssue(action, true, $"Missing required sound value for {action}."));
                return;
            }

            var anyValid = false;
            for (var i = 0; i < values.Count; i++)
            {
                if (TryResolveCustom(values[i], builtinRoot, vehicleRoot, action, out _, out var error, out var missing))
                    anyValid = true;
                else
                    issues.Add(new SoundIssue(action, !anyValid, error!, missing));
            }

            // A single surviving variant is enough to run; only escalate to required if none resolved.
            if (anyValid)
                for (var i = 0; i < issues.Count; i++)
                    if (issues[i].Action == action && issues[i].Required)
                        issues[i] = new SoundIssue(action, false, issues[i].Message, issues[i].MissingPath);
        }

        private static void CheckOptionalList(
            List<SoundIssue> issues, IReadOnlyList<string> values, string builtinRoot, string vehicleRoot, VehicleAction action)
        {
            if (values == null || values.Count == 0)
                return;
            for (var i = 0; i < values.Count; i++)
                if (!TryResolveCustom(values[i], builtinRoot, vehicleRoot, action, out _, out var error, out var missing))
                    issues.Add(new SoundIssue(action, false, error!, missing));
        }

        private static string GetDefaultFileName(VehicleAction action)
        {
            switch (action)
            {
                case VehicleAction.Engine: return "engine.wav";
                case VehicleAction.Start: return "start.wav";
                case VehicleAction.Horn: return "horn.wav";
                case VehicleAction.Throttle: return "throttle.wav";
                case VehicleAction.Crash: return "crash.wav";
                case VehicleAction.Brake: return "brake.wav";
                case VehicleAction.Backfire: return "backfire.wav";
                case VehicleAction.Stop: return "stop.wav";
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private static bool ContainsTraversal(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar);
            for (var i = 0; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                if (segment == "." || segment == "..")
                    return true;
            }

            return false;
        }

        private static bool IsInsideRoot(string rootFull, string candidate)
        {
            if (string.Equals(rootFull, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
            var rootWithSeparator = rootFull.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        private static string? ResolveCustomBuiltin(string token, string builtinRoot, VehicleAction action)
        {
            if (!int.TryParse(token.Substring(BuiltinPrefix.Length), out var index))
                return null;
            index -= 1;
            if (index < 0 || index >= VehicleCatalog.VehicleCount)
                return null;

            var parameters = VehicleCatalog.Vehicles[index];
            var file = parameters.GetSoundPath(action);
            if (!string.IsNullOrWhiteSpace(file))
                return Path.Combine(builtinRoot, file!);

            return ResolveOfficialFallback(builtinRoot, $"Vehicle{index + 1}", action);
        }
    }
}

