using System;
using System.Numerics;
using TopSpeed.Tracks.Geometry;

namespace TopSpeed.GeometryTest
{
    internal static class Program
    {
        private const float Epsilon = 0.001f;

        public static int Main(string[] args)
        {
            try
            {
                // If a path is provided, validate just that file
                if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                {
                    var path = args[0];
                    Console.WriteLine($"Validating: {path}");
                    return RunSingleFile(path) ? 0 : 1;
                }

                // Otherwise run all standard tests
                var geometry = BuildTestGeometry();
                var ok = RunChecks(geometry, "Generated geometry", expectedLength: 940f, expectBothCurvatures: true);
                var layoutOk = RunLayoutCheck("sample_layout.ttl", expectedLength: 1100f, expectBothCurvatures: true);
                var realisticOk = RunLayoutCheck("realistic_layout.ttl", expectedLength: 3480f, expectBothCurvatures: true);
                var loaderOk = RunLoaderCheck("realistic_layout.ttl", expectedLength: 3480f);
                var allLayoutsOk = RunAllLayouts();
                return ok && layoutOk && realisticOk && loaderOk && allLayoutsOk ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Geometry test failed with exception:");
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static bool RunSingleFile(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                return false;
            }

            var result = TrackLayoutFormat.ParseFile(path);
            if (!result.IsSuccess || result.Layout == null)
            {
                Console.WriteLine("[Parse] Failed:");
                foreach (var error in result.Errors)
                    Console.WriteLine($"  {error}");
                return false;
            }
            Console.WriteLine("[Parse] OK");

            var validation = TrackLayoutValidator.Validate(result.Layout);
            PrintValidation(validation, System.IO.Path.GetFileName(path));
            
            if (!validation.IsValid)
            {
                Console.WriteLine("[Validation] FAILED - has errors");
                return false;
            }

            var geometry = TrackGeometry.Build(result.Layout.Geometry);
            Console.WriteLine($"[Geometry] Built successfully");
            Console.WriteLine($"  Length: {geometry.LengthMeters:0.###}m");
            Console.WriteLine($"  Spans: {result.Layout.Geometry.Spans.Count}");
            Console.WriteLine($"  Sample Spacing: {geometry.SampleSpacingMeters:0.###}m");

            // Check closure
            var startPose = geometry.GetPose(0f);
            var endPose = geometry.GetPose(geometry.LengthMeters);
            var closureDistance = System.Numerics.Vector3.Distance(startPose.Position, endPose.Position);
            var headingDelta = Math.Abs(NormalizeAngle(endPose.HeadingRadians - startPose.HeadingRadians));
            var closureOk = closureDistance < 0.5f && headingDelta < 0.1f;
            Console.WriteLine($"[Closure] Distance: {closureDistance:0.###}m, Heading delta: {headingDelta:0.###} rad -> {(closureOk ? "OK" : "WARN")}");

            // Check for both curve directions
            var hasBothCurvatures = CheckCurvatureSamples(geometry, true, out var curvatureSummary);
            Console.WriteLine($"[Curvature] {curvatureSummary} -> {(hasBothCurvatures ? "OK" : "WARN")}");

            Console.WriteLine();
            Console.WriteLine(closureOk ? "=== VALIDATION PASSED ===" : "=== VALIDATION PASSED (with warnings) ===");
            return true;
        }

        private static TrackGeometry BuildTestGeometry()
        {
            var spans = new[]
            {
                TrackGeometrySpan.Straight(100f),
                TrackGeometrySpan.Clothoid(60f, 0f, 80f, TrackCurveDirection.Right, TrackCurveSeverity.Normal),
                TrackGeometrySpan.Arc(200f, 80f, TrackCurveDirection.Right, TrackCurveSeverity.Hard),
                TrackGeometrySpan.Clothoid(60f, 80f, 0f, TrackCurveDirection.Right, TrackCurveSeverity.Normal),
                TrackGeometrySpan.Straight(100f),
                TrackGeometrySpan.Clothoid(60f, 0f, 80f, TrackCurveDirection.Left, TrackCurveSeverity.Normal),
                TrackGeometrySpan.Arc(200f, 80f, TrackCurveDirection.Left, TrackCurveSeverity.Hard),
                TrackGeometrySpan.Clothoid(60f, 80f, 0f, TrackCurveDirection.Left, TrackCurveSeverity.Normal),
                TrackGeometrySpan.Straight(100f)
            };

            var spec = new TrackGeometrySpec(spans, sampleSpacingMeters: 0.5f, enforceClosure: true);
            return TrackGeometry.Build(spec);
        }

        private static bool RunChecks(TrackGeometry geometry, string label, float expectedLength, bool expectBothCurvatures)
        {
            var lengthOk = Math.Abs(geometry.LengthMeters - expectedLength) < 0.01f;
            Console.WriteLine($"[{label}] Length: {geometry.LengthMeters:0.###} (expected {expectedLength:0.###}) -> {(lengthOk ? "OK" : "FAIL")}");

            var startPose = geometry.GetPose(0f);
            var endPose = geometry.GetPose(geometry.LengthMeters);
            var closureDistance = Vector3.Distance(startPose.Position, endPose.Position);
            var headingDelta = Math.Abs(NormalizeAngle(endPose.HeadingRadians - startPose.HeadingRadians));
            var closureOk = closureDistance < 0.01f && headingDelta < 0.01f;
            Console.WriteLine($"[{label}] Closure: dist={closureDistance:0.#####}, headingΔ={headingDelta:0.#####} -> {(closureOk ? "OK" : "FAIL")}");

            var pose = geometry.GetPose(geometry.LengthMeters * 0.25f);
            var tangentOk = IsUnit(pose.Tangent);
            var rightOk = IsUnit(pose.Right);
            var upOk = IsUnit(pose.Up);
            var orthoOk = Math.Abs(Vector3.Dot(pose.Tangent, pose.Right)) < 0.001f &&
                          Math.Abs(Vector3.Dot(pose.Tangent, pose.Up)) < 0.001f &&
                          Math.Abs(Vector3.Dot(pose.Right, pose.Up)) < 0.001f;
            Console.WriteLine($"[{label}] Basis: T={tangentOk}, R={rightOk}, U={upOk}, orthogonal={orthoOk} -> {(tangentOk && rightOk && upOk && orthoOk ? "OK" : "FAIL")}");

            var edges = geometry.GetEdges(geometry.LengthMeters * 0.6f, 12f);
            var width = Vector3.Distance(edges.Left, edges.Right);
            var widthOk = Math.Abs(width - 12f) < 0.05f;
            Console.WriteLine($"[{label}] Edges: width={width:0.###} -> {(widthOk ? "OK" : "FAIL")}");

            var curvatureOk = CheckCurvatureSamples(geometry, expectBothCurvatures, out var curvatureSummary);
            Console.WriteLine($"[{label}] Curvature: {curvatureSummary} -> {(curvatureOk ? "OK" : "FAIL")}");

            var allOk = lengthOk && closureOk && tangentOk && rightOk && upOk && orthoOk && widthOk && curvatureOk;
            Console.WriteLine(allOk ? $"[{label}] Geometry checks passed." : $"[{label}] Geometry checks failed.");
            return allOk;
        }

        private static bool RunLayoutCheck(string fileName, float expectedLength, bool expectBothCurvatures)
        {
            var baseDir = AppContext.BaseDirectory;
            var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "Tracks", fileName));
            if (!System.IO.File.Exists(path))
            {
                var fallback = System.IO.Path.GetFullPath(System.IO.Path.Combine("top_speed_net", "Tracks", fileName));
                if (System.IO.File.Exists(fallback))
                {
                    path = fallback;
                }
                else
                {
                    Console.WriteLine($"[Layout] Layout not found: {path}");
                    return false;
                }
            }

            var result = TrackLayoutFormat.ParseFile(path);
            if (!result.IsSuccess || result.Layout == null)
            {
                Console.WriteLine("[Layout] Parse failed:");
                foreach (var error in result.Errors)
                    Console.WriteLine(error.ToString());
                return false;
            }

            var validation = TrackLayoutValidator.Validate(result.Layout);
            PrintValidation(validation, fileName);
            if (!validation.IsValid)
                return false;

            var geometry = TrackGeometry.Build(result.Layout.Geometry);
            var ok = RunChecks(geometry, $"Parsed layout ({fileName})", expectedLength: expectedLength, expectBothCurvatures: expectBothCurvatures);
            return ok;
        }

        private static bool RunLoaderCheck(string fileName, float expectedLength)
        {
            var baseDir = AppContext.BaseDirectory;
            var tracksPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "Tracks"));
            var source = new FileTrackLayoutSource(new[] { tracksPath });
            var loader = new TrackLayoutLoader(new[] { source });
            var result = loader.Load(new TrackLayoutLoadRequest(fileName, validate: true, buildGeometry: true, allowWarnings: true));

            if (!result.IsSuccess || result.Layout == null || result.Geometry == null)
            {
                Console.WriteLine("[Loader] Failed to load layout.");
                if (result.ParseErrors.Count > 0)
                {
                    Console.WriteLine("[Loader] Parse errors:");
                    foreach (var error in result.ParseErrors)
                        Console.WriteLine(error.ToString());
                }
                if (result.ValidationIssues.Count > 0)
                {
                    Console.WriteLine("[Loader] Validation issues:");
                    foreach (var issue in result.ValidationIssues)
                        Console.WriteLine(issue.ToString());
                }
                return false;
            }

            Console.WriteLine("[Loader] Load success.");
            return RunChecks(result.Geometry, $"Loader layout ({fileName})", expectedLength: expectedLength, expectBothCurvatures: true);
        }

        private static bool RunAllLayouts()
        {
            var baseDir = AppContext.BaseDirectory;
            var tracksPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "Tracks"));
            if (!System.IO.Directory.Exists(tracksPath))
            {
                var fallback = System.IO.Path.GetFullPath(System.IO.Path.Combine("top_speed_net", "Tracks"));
                if (!System.IO.Directory.Exists(fallback))
                {
                    Console.WriteLine("[Layouts] Tracks folder not found.");
                    return false;
                }
                tracksPath = fallback;
            }

            var files = System.IO.Directory.GetFiles(tracksPath, "*.ttl");
            if (files.Length == 0)
            {
                Console.WriteLine("[Layouts] No layouts found.");
                return false;
            }

            var allOk = true;
            foreach (var file in files)
            {
                var name = System.IO.Path.GetFileName(file);
                var result = TrackLayoutFormat.ParseFile(file);
                if (!result.IsSuccess || result.Layout == null)
                {
                    Console.WriteLine($"[Layouts] Parse failed: {name}");
                    foreach (var error in result.Errors)
                        Console.WriteLine(error.ToString());
                    allOk = false;
                    continue;
                }

                var validation = TrackLayoutValidator.Validate(result.Layout);
                PrintValidation(validation, name);
                if (!validation.IsValid)
                    allOk = false;
            }

            return allOk;
        }

        private static bool CheckCurvatureSamples(TrackGeometry geometry, bool expectBoth, out string summary)
        {
            var foundPositive = false;
            var foundNegative = false;
            var epsilon = 0.0005f;
            var length = geometry.LengthMeters;
            if (length <= 0f)
            {
                summary = "no length";
                return false;
            }

            var samples = 200;
            var step = Math.Max(1f, length / samples);
            for (var s = 0f; s <= length; s += step)
            {
                var curvature = geometry.CurvatureAt(s);
                if (curvature > epsilon) foundPositive = true;
                if (curvature < -epsilon) foundNegative = true;
                if (!expectBoth && (foundPositive || foundNegative))
                    break;
                if (expectBoth && foundPositive && foundNegative)
                    break;
            }

            if (expectBoth)
            {
                summary = $"positive={foundPositive}, negative={foundNegative}";
                return foundPositive && foundNegative;
            }

            summary = $"positive={foundPositive}, negative={foundNegative}";
            return foundPositive || foundNegative;
        }

        private static void PrintValidation(TrackLayoutValidationResult validation, string label)
        {
            if (validation.Issues.Count == 0)
            {
                Console.WriteLine($"[Validation {label}] OK");
                return;
            }

            Console.WriteLine($"[Validation {label}] Issues:");
            foreach (var issue in validation.Issues)
                Console.WriteLine(issue.ToString());
        }

        private static bool IsUnit(Vector3 vector)
        {
            var length = vector.Length();
            return Math.Abs(length - 1f) < 0.001f;
        }

        private static float NormalizeAngle(float angle)
        {
            var twoPi = (float)(Math.PI * 2.0);
            while (angle > Math.PI)
                angle -= twoPi;
            while (angle <= -Math.PI)
                angle += twoPi;
            return angle;
        }
    }
}
