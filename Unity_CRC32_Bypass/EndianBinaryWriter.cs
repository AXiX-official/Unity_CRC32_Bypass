using System.Text;
using System.Buffers.Binary;
using AssetStudio;

namespace Unity_CRC32_Bypass;

public class EndianBinaryWriter : BinaryWriter
{
    public EndianType Endian;
    
    public EndianBinaryWriter(Stream stream, EndianType endian = EndianType.BigEndian, bool leaveOpen = false) : base(stream, Encoding.UTF8, leaveOpen)
    {
        Endian = endian;
    }
    
    public long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    public void WriteStringToNull(string str)
    {
        Write(Encoding.UTF8.GetBytes(str));
        Write((byte)0);
    }
    
    public void WriteUInt32(uint value)
    {
        if (Endian == EndianType.BigEndian)
        {
            Write(BinaryPrimitives.ReverseEndianness(value));
        }
        else
        {
            Write(value);
        }
    }
    
    public void WriteInt64(long value)
    {
        if (Endian == EndianType.BigEndian)
        {
            Write(BinaryPrimitives.ReverseEndianness(value));
        }
        else
        {
            Write(value);
        }
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
    
    public void WriteInt32(int value)
    {
        if (Endian == EndianType.BigEndian)
        {
            Write(BinaryPrimitives.ReverseEndianness(value));
        }
        else
        {
            Write(value);
        }
    }
    
    public void WriteUInt16(ushort value)
    {
        if (Endian == EndianType.BigEndian)
        {
            Write(BinaryPrimitives.ReverseEndianness(value));
        }
        else
        {
            Write(value);
        }
    }
    
    public void WriteStream(Stream stream)
    {
        stream.CopyTo(BaseStream);
    }
}