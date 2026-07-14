using System;
using TopSpeed.Protocol;

namespace TopSpeed.Network
{
    internal static partial class ClientPacketSerializer
    {
        public static bool TryReadTrackPackageUploadBegin(byte[] data, out PacketTrackPackageUploadBegin packet)
        {
            packet = new PacketTrackPackageUploadBegin();
            if (data.Length < 2 + 4 + 2 + 2 + 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadBegin)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.UploadId = reader.ReadUInt32();
                packet.TrackId = reader.ReadString16();
                packet.Version = reader.ReadString16();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                packet.TotalBytes = reader.ReadUInt32();
                return PacketValidation.IsValidTrackPackageUploadBegin(packet);
            }
            catch
            {
                packet = new PacketTrackPackageUploadBegin();
                return false;
            }
        }

        public static bool TryReadTrackPackageUploadChunk(byte[] data, out PacketTrackPackageUploadChunk packet)
        {
            packet = new PacketTrackPackageUploadChunk();
            if (data.Length < 2 + 4 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadChunk)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.UploadId = reader.ReadUInt32();
                packet.ChunkIndex = reader.ReadUInt16();
                var length = reader.ReadUInt16();
                if (length == 0 || length > ProtocolConstants.MaxTrackPackageChunkBytes)
                    return false;
                if (data.Length != 2 + 4 + 2 + 2 + length)
                    return false;

                var bytes = new byte[length];
                for (var i = 0; i < length; i++)
                    bytes[i] = reader.ReadByte();
                packet.Data = bytes;
                return PacketValidation.IsValidTrackPackageUploadChunk(packet);
            }
            catch
            {
                packet = new PacketTrackPackageUploadChunk();
                return false;
            }
        }

        public static bool TryReadTrackPackageUploadEnd(byte[] data, out PacketTrackPackageUploadEnd packet)
        {
            packet = new PacketTrackPackageUploadEnd();
            if (data.Length < 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadEnd)
                return false;

            var reader = new PacketReader(data);
            reader.ReadByte();
            reader.ReadByte();
            packet.UploadId = reader.ReadUInt32();
            return PacketValidation.IsValidTrackPackageUploadEnd(packet);
        }

        public static bool TryReadTrackPackageUploadResult(byte[] data, out PacketTrackPackageUploadResult packet)
        {
            packet = new PacketTrackPackageUploadResult();
            if (data.Length < 2 + 4 + 1 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageUploadResult)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.UploadId = reader.ReadUInt32();
                packet.Status = (TrackPackageUploadStatus)reader.ReadByte();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                packet.Message = reader.ReadString16();
                return PacketValidation.IsValidTrackPackageUploadResult(packet);
            }
            catch
            {
                packet = new PacketTrackPackageUploadResult();
                return false;
            }
        }

        public static bool TryReadTrackPackageTransferBegin(byte[] data, out PacketTrackPackageTransferBegin packet)
        {
            packet = new PacketTrackPackageTransferBegin();
            if (data.Length < 2 + 2 + 2 + 2 + 4)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageTransferBegin)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.TrackId = reader.ReadString16();
                packet.Version = reader.ReadString16();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                packet.TotalBytes = reader.ReadUInt32();
                return PacketValidation.IsValidTrackPackageTransferBegin(packet);
            }
            catch
            {
                packet = new PacketTrackPackageTransferBegin();
                return false;
            }
        }

        public static bool TryReadTrackPackageTransferChunk(byte[] data, out PacketTrackPackageTransferChunk packet)
        {
            packet = new PacketTrackPackageTransferChunk();
            if (data.Length < 2 + 2 + 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageTransferChunk)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var rawHash = reader.ReadString16();
                packet.Hash = TrackPackageRef.NormalizeHash(rawHash);
                packet.ChunkIndex = reader.ReadUInt16();
                var length = reader.ReadUInt16();
                if (length == 0 || length > ProtocolConstants.MaxTrackPackageChunkBytes)
                    return false;
                if (data.Length != 2 + 2 + PacketWriter.MeasureString16(rawHash) + 2 + 2 + length)
                    return false;

                var bytes = new byte[length];
                for (var i = 0; i < length; i++)
                    bytes[i] = reader.ReadByte();
                packet.Data = bytes;
                return PacketValidation.IsValidTrackPackageTransferChunk(packet);
            }
            catch
            {
                packet = new PacketTrackPackageTransferChunk();
                return false;
            }
        }

        public static bool TryReadTrackPackageTransferEnd(byte[] data, out PacketTrackPackageTransferEnd packet)
        {
            packet = new PacketTrackPackageTransferEnd();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageTransferEnd)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                return PacketValidation.IsValidTrackPackageTransferEnd(packet);
            }
            catch
            {
                packet = new PacketTrackPackageTransferEnd();
                return false;
            }
        }

        public static bool TryReadTrackPackageReady(byte[] data, out PacketTrackPackageReady packet)
        {
            packet = new PacketTrackPackageReady();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageReady)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                packet.Hash = TrackPackageRef.NormalizeHash(reader.ReadString16());
                return PacketValidation.IsValidTrackPackageReady(packet);
            }
            catch
            {
                packet = new PacketTrackPackageReady();
                return false;
            }
        }

        public static bool TryReadTrackPackageCatalogRequest(byte[] data, out PacketTrackPackageCatalogRequest packet)
        {
            packet = new PacketTrackPackageCatalogRequest();
            if (data.Length != 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageCatalogRequest)
                return false;
            return PacketValidation.IsValidTrackPackageCatalogRequest(packet);
        }

        public static bool TryReadTrackPackageCatalog(byte[] data, out PacketTrackPackageCatalog packet)
        {
            packet = new PacketTrackPackageCatalog();
            if (data.Length < 2 + 2)
                return false;
            if (data[0] != ProtocolConstants.Version || data[1] != (byte)Command.TrackPackageCatalog)
                return false;

            try
            {
                var reader = new PacketReader(data);
                reader.ReadByte();
                reader.ReadByte();
                var count = reader.ReadUInt16();
                if (count > ProtocolConstants.MaxTrackPackageCatalogEntries)
                    return false;

                var tracks = new PacketTrackPackageCatalogEntry[count];
                for (var i = 0; i < count; i++)
                {
                    var track = ReadCatalogTrackRef(ref reader);
                    var displayName = reader.ReadString16();
                    var hasPitArea = reader.ReadBool();
                    tracks[i] = new PacketTrackPackageCatalogEntry
                    {
                        Track = track,
                        DisplayName = displayName,
                        HasPitArea = hasPitArea
                    };
                }

                packet.Tracks = tracks;
                return PacketValidation.IsValidTrackPackageCatalog(packet);
            }
            catch
            {
                packet = new PacketTrackPackageCatalog();
                return false;
            }
        }
    }
}
