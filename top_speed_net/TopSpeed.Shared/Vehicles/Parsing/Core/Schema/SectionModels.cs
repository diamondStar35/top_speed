using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles.Parsing
{
    public static partial class VehicleTsvParser
    {
        private sealed class Entry
        {
            public Entry(string value, int line)
            {
                Value = value;
                Line = line;
            }

            public string Value { get; }
            public int Line { get; }
        }

        private sealed class Section
        {
            public Section(string name, int line)
            {
                Name = name;
                Line = line;
                Entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            }

            public string Name { get; }
            public int Line { get; }
            public Dictionary<string, Entry> Entries { get; }
        }
    }
}

