using System;
using System.IO;
using System.Linq;
using TopSpeed.Tracks.Map;

namespace TopSpeed.GeometryTest
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    var path = args[0];
                    return ValidateFile(path) ? 0 : 1;
                }

                var tracksRoot = Path.Combine(AppContext.BaseDirectory, "Tracks");
                if (!Directory.Exists(tracksRoot))
                {
                    Console.WriteLine($"Tracks folder not found: {tracksRoot}");
                    return 1;
                }

                var files = Directory.GetFiles(tracksRoot, "*.tsm").OrderBy(p => p).ToList();
                if (files.Count == 0)
                {
                    Console.WriteLine("No .tsm files found.");
                    return 0;
                }

                var allOk = true;
                foreach (var file in files)
                {
                    if (!ValidateFile(file))
                        allOk = false;
                }

                return allOk ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Map validation failed with exception:");
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static bool ValidateFile(string path)
        {
            Console.WriteLine($"Validating: {path}");
            var validation = TrackMapValidator.ValidateFile(path, new TrackMapValidationOptions
            {
                RequireSafeZones = false,
                RequireIntersections = false,
                TreatUnreachableCellsAsErrors = false
            });

            if (validation.Issues.Count == 0)
            {
                Console.WriteLine("  OK");
                Console.WriteLine();
                return true;
            }

            foreach (var issue in validation.Issues)
                Console.WriteLine($"  {issue}");

            Console.WriteLine();
            return validation.IsValid;
        }
    }
}
