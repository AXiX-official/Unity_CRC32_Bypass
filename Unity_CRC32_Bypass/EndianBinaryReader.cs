﻿// This file is copied from: https://github.com/RazTools/Studio/tree/main/AssetStudio/EndianBinaryReader.cs

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetStudio
{
    public class EndianBinaryReader : BinaryReader
    {
        private readonly byte[] buffer;

        public EndianType Endian;

        public EndianBinaryReader(Stream stream, EndianType endian = EndianType.BigEndian, bool leaveOpen = false) : base(stream, Encoding.UTF8, leaveOpen)
        {
            Endian = endian;
            buffer = new byte[8];
        }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public long Length => BaseStream.Length;
        public long Remaining => Length - Position;

        public override short ReadInt16()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 2);
                return BinaryPrimitives.ReadInt16BigEndian(buffer);
            }
            return base.ReadInt16();
        }

        public override int ReadInt32()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 4);
                return BinaryPrimitives.ReadInt32BigEndian(buffer);
            }
            return base.ReadInt32();
        }

        public override long ReadInt64()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 8);
                return BinaryPrimitives.ReadInt64BigEndian(buffer);
            }
            return base.ReadInt64();
        }

        public override ushort ReadUInt16()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 2);
                return BinaryPrimitives.ReadUInt16BigEndian(buffer);
            }
            return base.ReadUInt16();
        }

        public override uint ReadUInt32()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 4);
                return BinaryPrimitives.ReadUInt32BigEndian(buffer);
            }
            return base.ReadUInt32();
        }

        public override ulong ReadUInt64()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 8);
                return BinaryPrimitives.ReadUInt64BigEndian(buffer);
            }
            return base.ReadUInt64();
        }

        public override float ReadSingle()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 4);
                Array.Reverse(buffer, 0, 4);
                return BitConverter.ToSingle(buffer, 0);
            }
            return base.ReadSingle();
        }

        public override double ReadDouble()
        {
            if (Endian == EndianType.BigEndian)
            {
                Read(buffer, 0, 8);
                Array.Reverse(buffer);
                return BitConverter.ToDouble(buffer, 0);
            }
            return base.ReadDouble();
        }
        public override byte[] ReadBytes(int count)
        {
            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = ArrayPool<byte>.Shared.Rent(0x1000);
            List<byte> result = new List<byte>();
            do
            {
                var readNum = Math.Min(count, buffer.Length);
                int n = Read(buffer, 0, readNum);
                if (n == 0)
                {
                    break;
                }

                result.AddRange(buffer[..n]);
                count -= n;
            } while (count > 0);

            ArrayPool<byte>.Shared.Return(buffer);
            return result.ToArray();
        }

        public void AlignStream()
        {
            AlignStream(4);
        }

        public void AlignStream(int alignment)
        {
            var pos = Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                Position += alignment - mod;
            }
        }

        public string ReadAlignedString()
        {
            var result = "";
            var length = ReadInt32();
            if (length > 0 && length <= Remaining)
            {
                var stringData = ReadBytes(length);
                result = Encoding.UTF8.GetString(stringData);
            }
            AlignStream();
            return result;
        }

        public string ReadStringToNull(int maxLength = 32767)
        {
            var bytes = new List<byte>();
            int count = 0;
            while (Remaining > 0 && count < maxLength)
            {
                var b = ReadByte();
                if (b == 0)
                {
                    break;
                }
                bytes.Add(b);
                count++;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        
        internal T[] ReadArray<T>(Func<T> del, int length)
        {
            if (length < 0x1000)
            {
                var array = new T[length];
                for (int i = 0; i < length; i++)
                {
                    array[i] = del();
                }
                return array;
            }
            else
            {
                var list = new List<T>();
                for (int i = 0; i < length; i++)
                {
                    list.Add(del());
                }
                return list.ToArray();
            }
        }

        public bool[] ReadBooleanArray(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadBoolean, length);
        }

        public byte[] ReadUInt8Array(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadBytes(length);
        }

        public short[] ReadInt16Array(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadInt16, length);
        }

        public ushort[] ReadUInt16Array(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadUInt16, length);
        }

        public int[] ReadInt32Array(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadInt32, length);
        }

        public uint[] ReadUInt32Array(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadUInt32, length);
        }

        public uint[][] ReadUInt32ArrayArray(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(() => ReadUInt32Array(), length);
        }

        public float[] ReadSingleArray(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadSingle, length);
        }

        public string[] ReadStringArray(int length = 0)
        {
            if (length == 0)
            {
                length = ReadInt32();
            }
            return ReadArray(ReadAlignedString, length);
        }
    }
}
