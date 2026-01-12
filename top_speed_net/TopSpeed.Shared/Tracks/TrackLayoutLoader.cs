using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TopSpeed.Tracks.Geometry
{
    public sealed class TrackLayoutLoadRequest
    {
        public string Identifier { get; }
        public bool Validate { get; }
        public bool BuildGeometry { get; }
        public bool AllowWarnings { get; }
        public TrackLayoutValidationOptions? ValidationOptions { get; }

        public TrackLayoutLoadRequest(
            string identifier,
            bool validate = true,
            bool buildGeometry = true,
            bool allowWarnings = true,
            TrackLayoutValidationOptions? validationOptions = null)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier is required.", nameof(identifier));

            Identifier = identifier.Trim();
            Validate = validate;
            BuildGeometry = buildGeometry;
            AllowWarnings = allowWarnings;
            ValidationOptions = validationOptions;
        }
    }

    public sealed class TrackLayoutLoadResult
    {
        public string Identifier { get; }
        public string? SourceName { get; }
        public TrackLayout? Layout { get; }
        public TrackGeometry? Geometry { get; }
        public IReadOnlyList<TrackLayoutError> ParseErrors { get; }
        public IReadOnlyList<TrackLayoutIssue> ValidationIssues { get; }
        public bool IsSuccess { get; }

        public TrackLayoutLoadResult(
            string identifier,
            string? sourceName,
            TrackLayout? layout,
            TrackGeometry? geometry,
            IReadOnlyList<TrackLayoutError> parseErrors,
            IReadOnlyList<TrackLayoutIssue> validationIssues)
        {
            Identifier = identifier;
            SourceName = sourceName;
            Layout = layout;
            Geometry = geometry;
            ParseErrors = parseErrors;
            ValidationIssues = validationIssues;
            IsSuccess = parseErrors.Count == 0 && layout != null && (validationIssues.Count == 0 || geometry != null);
        }
    }

    public sealed class TrackLayoutLoader
    {
        private readonly List<ITrackLayoutSource> _sources = new List<ITrackLayoutSource>();
        private readonly TrackLayoutCache _cache;

        public TrackLayoutLoader(IEnumerable<ITrackLayoutSource> sources, TrackLayoutCache? cache = null)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            _sources.AddRange(sources);
            _cache = cache ?? new TrackLayoutCache();
        }

        public TrackLayoutLoadResult Load(TrackLayoutLoadRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_cache.TryGet(request.Identifier, out var cached) && cached != null)
            {
                return cached;
            }

            foreach (var source in _sources)
            {
                if (!source.TryOpen(request.Identifier, out var entry))
                    continue;

                using (entry)
                {
                    var parseResult = TrackLayoutFormat.ParseLines(ReadAllLines(entry.Stream), entry.SourceName);
                    if (!parseResult.IsSuccess || parseResult.Layout == null)
                    {
                        var parseErrors = parseResult.Errors ?? Array.Empty<TrackLayoutError>();
                        var result = new TrackLayoutLoadResult(request.Identifier, entry.SourceName, null, null, parseErrors, Array.Empty<TrackLayoutIssue>());
                        _cache.Store(request.Identifier, entry.LastModifiedUtc, result);
                        return result;
                    }

                    var layout = parseResult.Layout;
                    IReadOnlyList<TrackLayoutIssue> validationIssues = Array.Empty<TrackLayoutIssue>();
                    if (request.Validate)
                    {
                        var validation = TrackLayoutValidator.Validate(layout, request.ValidationOptions);
                        validationIssues = validation.Issues;
                        if (!validation.IsValid || (!request.AllowWarnings && validationIssues.Count > 0))
                        {
                            var result = new TrackLayoutLoadResult(request.Identifier, entry.SourceName, layout, null, Array.Empty<TrackLayoutError>(), validationIssues);
                            _cache.Store(request.Identifier, entry.LastModifiedUtc, result);
                            return result;
                        }
                    }

                    TrackGeometry? geometry = null;
                    if (request.BuildGeometry)
                        geometry = TrackGeometry.Build(layout.Geometry);

                    var success = new TrackLayoutLoadResult(request.Identifier, entry.SourceName, layout, geometry, Array.Empty<TrackLayoutError>(), validationIssues);
                    _cache.Store(request.Identifier, entry.LastModifiedUtc, success);
                    return success;
                }
            }

            return new TrackLayoutLoadResult(request.Identifier, null, null, null, Array.Empty<TrackLayoutError>(), Array.Empty<TrackLayoutIssue>());
        }

        private static IEnumerable<string> ReadAllLines(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}
