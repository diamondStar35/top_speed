using System;
using System.IO;

namespace TopSpeed.Core
{
    internal static class AssetPaths
    {
        private static string? _root;

        public static string Root
        {
            get
            {
                if (_root != null)
                    return _root;

                var baseDir = AppContext.BaseDirectory;
                _root = baseDir;
                return _root;
            }
        }

        public static string SoundsRoot => Path.Combine(Root, "Sounds");
    }
}
