using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TopSpeed.Protocol
{
    public static class VehiclePackageCodec
    {
        private const byte FormatVersion = 1;

        public static byte[] Serialize(VehiclePackagePayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                WritePayload(writer, payload, includeHash: true);
                // Trailing metadata, deliberately outside WritePayload so it is NOT part of the
                // content hash (ComputeHash only calls WritePayload).
                var manifest = payload.Manifest ?? new VehiclePackageManifest();
                writer.Write(manifest.TsvFileName ?? string.Empty);
                writer.Write(manifest.FolderName ?? string.Empty);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public static bool TryDeserialize(byte[] bytes, out VehiclePackagePayload payload, out string error)
        {
            payload = new VehiclePackagePayload();
            error = string.Empty;

            if (bytes == null || bytes.Length == 0)
            {
                error = "Vehicle package payload is empty.";
                return false;
            }

            try
            {
                using (var ms = new MemoryStream(bytes, writable: false))
                using (var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true))
                {
                    payload = ReadPayload(reader);
                    if (ms.Position != ms.Length)
                    {
                        error = "Vehicle package payload contains trailing bytes.";
                        return false;
                    }
                }
            }
            catch (Exception ex) when (ex is EndOfStreamException || ex is IOException || ex is InvalidDataException || ex is ArgumentException)
            {
                error = ex.Message;
                payload = new VehiclePackagePayload();
                return false;
            }

            return TryValidate(payload, out error);
        }

        public static string ComputeHash(VehiclePackagePayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                WritePayload(writer, payload, includeHash: false);
                writer.Flush();
                return ComputeHash(ms.ToArray());
            }
        }

        public static string ComputeHash(byte[] canonicalBytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(canonicalBytes ?? Array.Empty<byte>());
                var builder = new StringBuilder(hash.Length * 2);
                for (var i = 0; i < hash.Length; i++)
                    builder.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        public static string NormalizeAssetKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var normalized = key.Trim().Replace('\\', '/').TrimStart('/');
            if (normalized.Length == 0 || normalized.IndexOf(':') >= 0)
                return string.Empty;

            var segments = normalized.Split('/');
            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "." || segments[i] == ".." || segments[i].Length == 0)
                    return string.Empty;
            }

            return normalized;
        }

        public static bool TryValidate(VehiclePackagePayload payload, out string error)
        {
            error = string.Empty;
            if (payload == null)
            {
                error = "Vehicle package payload is missing.";
                return false;
            }

            var manifest = payload.Manifest ?? new VehiclePackageManifest();
            if (string.IsNullOrWhiteSpace(manifest.VehicleId) || manifest.VehicleId.Length > ProtocolConstants.MaxVehicleIdLength)
            {
                error = "Vehicle package id is invalid.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(manifest.Version) || manifest.Version.Length > ProtocolConstants.MaxVehicleVersionLength)
            {
                error = "Vehicle package version is invalid.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(payload.TsvText))
            {
                error = "Vehicle package is missing its definition text.";
                return false;
            }

            return true;
        }

        private static void WritePayload(BinaryWriter writer, VehiclePackagePayload payload, bool includeHash)
        {
            var manifest = payload.Manifest ?? new VehiclePackageManifest();
            writer.Write(FormatVersion);
            writer.Write(manifest.VehicleId ?? string.Empty);
            writer.Write(manifest.Version ?? string.Empty);
            writer.Write(includeHash ? VehiclePackageRef.NormalizeHash(manifest.Hash) : string.Empty);
            writer.Write(manifest.DisplayName ?? string.Empty);
            writer.Write(payload.TsvText ?? string.Empty);
            WriteAssets(writer, payload.AssetBlobs ?? new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase));
        }

        private static VehiclePackagePayload ReadPayload(BinaryReader reader)
        {
            var format = reader.ReadByte();
            if (format != FormatVersion)
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Unsupported vehicle package payload format '{0}'.", format));

            var payload = new VehiclePackagePayload
            {
                Manifest = new VehiclePackageManifest
                {
                    VehicleId = reader.ReadString(),
                    Version = reader.ReadString(),
                    Hash = VehiclePackageRef.NormalizeHash(reader.ReadString()),
                    DisplayName = reader.ReadString()
                },
                TsvText = reader.ReadString()
            };
            payload.AssetBlobs = ReadAssets(reader);
            // Trailing metadata written by Serialize (see WritePayload comment). Only Serialize output
            // is ever deserialized, so these are always present here.
            payload.Manifest.TsvFileName = reader.ReadString();
            payload.Manifest.FolderName = reader.ReadString();
            return payload;
        }

        private static void WriteAssets(BinaryWriter writer, IReadOnlyDictionary<string, byte[]> assets)
        {
            var ordered = assets.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            writer.Write(ordered.Length);
            for (var i = 0; i < ordered.Length; i++)
            {
                writer.Write(NormalizeAssetKey(ordered[i].Key));
                var data = ordered[i].Value ?? Array.Empty<byte>();
                writer.Write(data.Length);
                writer.Write(data);
            }
        }

        private static IReadOnlyDictionary<string, byte[]> ReadAssets(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            if (count < 0)
                throw new InvalidDataException("Invalid asset count.");

            var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                var key = NormalizeAssetKey(reader.ReadString());
                if (string.IsNullOrWhiteSpace(key))
                    throw new InvalidDataException("Invalid asset key.");
                var length = reader.ReadInt32();
                if (length < 0)
                    throw new InvalidDataException("Invalid asset blob length.");
                var data = reader.ReadBytes(length);
                if (data.Length != length)
                    throw new EndOfStreamException("Unexpected end of asset blob.");
                map[key] = data;
            }

            return map;
        }
    }
}
