using System;
using System.Text;

namespace TopSpeed.Protocol
{
    public struct PacketReader
    {
        private readonly byte[] _data;
        private int _offset;

        public PacketReader(byte[] data)
        {
            _data = data;
            _offset = 0;
        }

        public byte ReadByte() => _data[_offset++];
        public bool ReadBool() => ReadByte() != 0;

        public ushort ReadUInt16()
        {
            var value = (ushort)(_data[_offset] | (_data[_offset + 1] << 8));
            _offset += 2;
            return value;
        }

        public uint ReadUInt32()
        {
            var value = (uint)(_data[_offset]
                | (_data[_offset + 1] << 8)
                | (_data[_offset + 2] << 16)
                | (_data[_offset + 3] << 24));
            _offset += 4;
            return value;
        }

        public int ReadInt32()
        {
            var value = _data[_offset]
                | (_data[_offset + 1] << 8)
                | (_data[_offset + 2] << 16)
                | (_data[_offset + 3] << 24);
            _offset += 4;
            return value;
        }

        public string ReadFixedString(int length)
        {
            var value = Encoding.ASCII.GetString(_data, _offset, length);
            _offset += length;
            var nullIndex = value.IndexOf('\0');
            return nullIndex >= 0 ? value.Substring(0, nullIndex) : value.Trim();
        }
    }

    public struct PacketWriter
    {
        private readonly byte[] _buffer;
        private int _offset;

        public PacketWriter(byte[] buffer)
        {
            _buffer = buffer;
            _offset = 0;
        }

        public void WriteByte(byte value) => _buffer[_offset++] = value;
        public void WriteBool(bool value) => WriteByte((byte)(value ? 1 : 0));

        public void WriteUInt16(ushort value)
        {
            _buffer[_offset++] = (byte)(value & 0xFF);
            _buffer[_offset++] = (byte)(value >> 8);
        }

        public void WriteUInt32(uint value)
        {
            _buffer[_offset++] = (byte)(value & 0xFF);
            _buffer[_offset++] = (byte)((value >> 8) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 16) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 24) & 0xFF);
        }

        public void WriteInt32(int value)
        {
            _buffer[_offset++] = (byte)(value & 0xFF);
            _buffer[_offset++] = (byte)((value >> 8) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 16) & 0xFF);
            _buffer[_offset++] = (byte)((value >> 24) & 0xFF);
        }

        public void WriteFixedString(string value, int length)
        {
            var bytes = Encoding.ASCII.GetBytes(value ?? string.Empty);
            var count = Math.Min(length, bytes.Length);
            Array.Copy(bytes, 0, _buffer, _offset, count);
            for (var i = count; i < length; i++)
                _buffer[_offset + i] = 0;
            _offset += length;
        }
    }
}
