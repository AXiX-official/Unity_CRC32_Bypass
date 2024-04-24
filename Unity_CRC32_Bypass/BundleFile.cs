// This file is based on: https://github.com/RazTools/Studio/tree/main/AssetStudio/BundleFile.cs

//sing ZstdSharp;
using System.Text;
using System.Text.RegularExpressions;
using System.Buffers;
using AssetStudio;

namespace Unity_CRC32_Bypass;

[Flags]
public enum ArchiveFlags
{
    CompressionTypeMask = 0x3f,
    BlocksAndDirectoryInfoCombined = 0x40,
    BlocksInfoAtTheEnd = 0x80,
    OldWebPluginCompatibility = 0x100,
    BlockInfoNeedPaddingAtStart = 0x200,
    UnityCNEncryption = 0x400
}

[Flags]
public enum StorageBlockFlags
{
    CompressionTypeMask = 0x3f,
    Streamed = 0x40,
}

public enum CompressionType
{
    None,
    Lzma,
    Lz4,
    Lz4HC,
    Lzham,
    Lz4Mr0k,
    Lz4Inv = 5,
    Zstd = 5,
    Lz4Lit4 = 4,
    Lz4Lit5 = 5,
}

public class BundleFile
{
    public class Header
    {
        public string signature;
        public uint version;
        public string unityVersion;
        public string unityRevision;
        public long size;
        public uint compressedBlocksInfoSize;
        public uint uncompressedBlocksInfoSize;
        public ArchiveFlags flags;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"signature: {signature} | ");
            sb.Append($"version: {version} | ");
            sb.Append($"unityVersion: {unityVersion} | ");
            sb.Append($"unityRevision: {unityRevision} | ");
            sb.Append($"size: 0x{size:X8} | ");
            sb.Append($"compressedBlocksInfoSize: 0x{compressedBlocksInfoSize:X8} | ");
            sb.Append($"uncompressedBlocksInfoSize: 0x{uncompressedBlocksInfoSize:X8} | ");
            sb.Append($"flags: 0x{(int)flags:X8}");
            return sb.ToString();
        }
        
        public void WriteToStream(EndianBinaryWriter writer)
        {
            writer.WriteStringToNull(signature);
            writer.WriteUInt32(version);
            writer.WriteStringToNull(unityVersion);
            writer.WriteStringToNull(unityRevision);
            writer.WriteInt64(size);
            writer.WriteUInt32(compressedBlocksInfoSize);
            writer.WriteUInt32(uncompressedBlocksInfoSize);
            writer.WriteUInt32((uint)flags);
        }
    }

    public class StorageBlock
    {
        public uint compressedSize;
        public uint uncompressedSize;
        public StorageBlockFlags flags;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"compressedSize: 0x{compressedSize:X8} | ");
            sb.Append($"uncompressedSize: 0x{uncompressedSize:X8} | ");
            sb.Append($"flags: 0x{(int)flags:X8}");
            return sb.ToString();
        }
        
        public void WriteToStream(EndianBinaryWriter writer)
        {
            writer.WriteUInt32(uncompressedSize);
            writer.WriteUInt32(compressedSize);
            writer.WriteUInt16((ushort)flags);
        }
    }

    public class Node
    {
        public long offset;
        public long size;
        public uint flags;
        public string path;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"offset: 0x{offset:X8} | ");
            sb.Append($"size: 0x{size:X8} | ");
            sb.Append($"flags: {flags} | ");
            sb.Append($"path: {path}");
            return sb.ToString();
        }
        
        public void WriteToStream(EndianBinaryWriter writer)
        {
            writer.WriteInt64(offset);
            writer.WriteInt64(size);
            writer.WriteUInt32(flags);
            writer.WriteStringToNull(path);
        }
    }
    
    //private UnityCN UnityCN;

    public Header m_Header;
    private List<Node> m_DirectoryInfo;
    private List<StorageBlock> m_BlocksInfo;

    public List<StreamFile> fileList;
    
    private bool HasUncompressedDataHash = true;
    private bool HasBlockInfoNeedPaddingAtStart = true;
    
    private Stream blocksStream;
    
    public uint crc32;

    public BundleFile(string path)
    {
        using var reader = new FileReader(path);
        m_Header = ReadBundleHeader(reader);
        ReadHeader(reader);
        /*if (game.Type.IsUnityCN())
        {
            ReadUnityCN(reader);
        }*/
        ReadBlocksInfoAndDirectory(reader);
        blocksStream = CreateBlocksStream(reader.FullPath);
        ReadBlocks(reader, blocksStream);
        crc32 = CRC32.CRC(blocksStream);
    }

    public void append(uint fixCRC)
    {
        // fixCRC -> bytes,big endian
        var fixCRCBytes = BitConverter.GetBytes(fixCRC);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(fixCRCBytes);
        }
        Console.WriteLine($"Fix CRC32: 0x{fixCRC:X8}");
        m_BlocksInfo[^1].uncompressedSize += 4;
        m_BlocksInfo[^1].compressedSize += 4;
        m_DirectoryInfo[^1].size += 4;
        blocksStream.SetLength(blocksStream.Length + 4);
        blocksStream.Position = blocksStream.Length - 4;
        blocksStream.Write(fixCRCBytes, 0, 4);
        blocksStream.Position = 0;
        crc32 = CRC32.CRC(blocksStream);
    }

    private Header ReadBundleHeader(FileReader reader)
    {
        Header header = new Header();
        header.signature = reader.ReadStringToNull(20);
        
        switch (header.signature)
        {
            case "UnityFS":
                header.version = reader.ReadUInt32();
                header.unityVersion = reader.ReadStringToNull();
                header.unityRevision = reader.ReadStringToNull();
                break;
            default:
                throw new Exception("Unsupported signature, only UnityFS is supported.");
        }

        return header;
    }
    
    public void WriteToFile(string path)
    {
        using var writer = new EndianBinaryWriter(new FileStream(path, FileMode.Create));
        m_Header.WriteToStream(writer);
        if (m_Header.version >= 7)
        {
            writer.AlignStream(16);
        }
        bool kArchiveBlocksInfoAtTheEnd = false;
        if ((m_Header.flags & ArchiveFlags.BlocksInfoAtTheEnd) != 0) //kArchiveBlocksInfoAtTheEnd
        {
            kArchiveBlocksInfoAtTheEnd = true;
        }
        else
        {
            //assert CompressionType.None
            if (HasUncompressedDataHash)
            {
                writer.Write(new byte[16]);
            }
            writer.WriteInt32(m_BlocksInfo.Count);
            foreach (var block in m_BlocksInfo)
            {
                block.WriteToStream(writer);
            }
            writer.WriteInt32(m_DirectoryInfo.Count);
            foreach (var node in m_DirectoryInfo)
            {
                node.WriteToStream(writer);
            }
        }
        if (HasBlockInfoNeedPaddingAtStart && (m_Header.flags & ArchiveFlags.BlockInfoNeedPaddingAtStart) != 0)
        {
            writer.AlignStream(16);
        }
        blocksStream.Position = 0;
        writer.WriteStream(blocksStream);
        if (kArchiveBlocksInfoAtTheEnd)
        {
            //assert CompressionType.None
            if (HasUncompressedDataHash)
            {
                writer.Write(new byte[16]);
            }
            writer.WriteInt32(m_BlocksInfo.Count);
            foreach (var block in m_BlocksInfo)
            {
                block.WriteToStream(writer);
            }
            writer.WriteInt32(m_DirectoryInfo.Count);
            foreach (var node in m_DirectoryInfo)
            {
                node.WriteToStream(writer);
            }
        }
    }

    private Stream CreateBlocksStream(string path)
    {
        Stream blocksStream;
        var uncompressedSizeSum = m_BlocksInfo.Sum(x => x.uncompressedSize);
        if (uncompressedSizeSum >= int.MaxValue)
        {
            /*var memoryMappedFile = MemoryMappedFile.CreateNew(null, uncompressedSizeSum);
            assetsDataStream = memoryMappedFile.CreateViewStream();*/
            blocksStream = new FileStream(path + ".temp", FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
        }
        else
        {
            blocksStream = new MemoryStream((int)uncompressedSizeSum);
        }
        return blocksStream;
    }
    
    private void ReadHeader(FileReader reader)
    {
        m_Header.size = reader.ReadInt64();
        m_Header.compressedBlocksInfoSize = reader.ReadUInt32();
        m_Header.uncompressedBlocksInfoSize = reader.ReadUInt32();
        m_Header.flags = (ArchiveFlags)reader.ReadUInt32();
    }

    private void ReadUnityCN(FileReader reader)
    {
        ArchiveFlags mask;

        var version = ParseVersion();
        //Flag changed it in these versions
        if (version[0] < 2020 || //2020 and earlier
            (version[0] == 2020 && version[1] == 3 && version[2] <= 34) || //2020.3.34 and earlier
            (version[0] == 2021 && version[1] == 3 && version[2] <= 2) || //2021.3.2 and earlier
            (version[0] == 2022 && version[1] == 3 && version[2] <= 1)) //2022.3.1 and earlier
        {
            mask = ArchiveFlags.BlockInfoNeedPaddingAtStart;
            HasBlockInfoNeedPaddingAtStart = false;
        }
        else
        {
            mask = ArchiveFlags.UnityCNEncryption;
            HasBlockInfoNeedPaddingAtStart = true;
        }
        

        if ((m_Header.flags & mask) != 0)
        {
            //UnityCN = new UnityCN(reader);
        }
    }

    private void ReadBlocksInfoAndDirectory(FileReader reader)
    {
        byte[] blocksInfoBytes;
        if (m_Header.version >= 7)
        {
            reader.AlignStream(16);
        }
        if ((m_Header.flags & ArchiveFlags.BlocksInfoAtTheEnd) != 0) //kArchiveBlocksInfoAtTheEnd
        {
            var position = reader.Position;
            reader.Position = reader.BaseStream.Length - m_Header.compressedBlocksInfoSize;
            blocksInfoBytes = reader.ReadBytes((int)m_Header.compressedBlocksInfoSize);
            reader.Position = position;
        }
        else //0x40 BlocksAndDirectoryInfoCombined
        {
            blocksInfoBytes = reader.ReadBytes((int)m_Header.compressedBlocksInfoSize);
        }
        MemoryStream blocksInfoUncompresseddStream;
        var blocksInfoBytesSpan = blocksInfoBytes.AsSpan(0, (int)m_Header.compressedBlocksInfoSize);
        var uncompressedSize = m_Header.uncompressedBlocksInfoSize;
        var compressionType = (CompressionType)(m_Header.flags & ArchiveFlags.CompressionTypeMask);

        switch (compressionType) //kArchiveCompressionTypeMask
        {
            case CompressionType.None: //None
                {
                    blocksInfoUncompresseddStream = new MemoryStream(blocksInfoBytes);
                    break;
                }
            default:
                throw new IOException($"Unsupported compression type {compressionType}");
        }
        using (var blocksInfoReader = new EndianBinaryReader(blocksInfoUncompresseddStream))
        {
            if (HasUncompressedDataHash)
            {
                var uncompressedDataHash = blocksInfoReader.ReadBytes(16);
            }
            var blocksInfoCount = blocksInfoReader.ReadInt32();
            m_BlocksInfo = new List<StorageBlock>();

            for (int i = 0; i < blocksInfoCount; i++)
            {
                m_BlocksInfo.Add(new StorageBlock
                {
                    uncompressedSize = blocksInfoReader.ReadUInt32(),
                    compressedSize = blocksInfoReader.ReadUInt32(),
                    flags = (StorageBlockFlags)blocksInfoReader.ReadUInt16()
                });


            }

            var nodesCount = blocksInfoReader.ReadInt32();
            m_DirectoryInfo = new List<Node>();

            for (int i = 0; i < nodesCount; i++)
            {
                m_DirectoryInfo.Add(new Node
                {
                    offset = blocksInfoReader.ReadInt64(),
                    size = blocksInfoReader.ReadInt64(),
                    flags = blocksInfoReader.ReadUInt32(),
                    path = blocksInfoReader.ReadStringToNull(),
                });


            }
        }
        if (HasBlockInfoNeedPaddingAtStart && (m_Header.flags & ArchiveFlags.BlockInfoNeedPaddingAtStart) != 0)
        {
            reader.AlignStream(16);
        }
    }

    private void ReadBlocks(FileReader reader, Stream blocksStream)
    {


        for (int i = 0; i < m_BlocksInfo.Count; i++)
        {

            var blockInfo = m_BlocksInfo[i];
            var compressionType = (CompressionType)(blockInfo.flags & StorageBlockFlags.CompressionTypeMask);

            switch (compressionType) //kStorageBlockCompressionTypeMask
            {
                case CompressionType.None: //None
                    {
                        reader.BaseStream.CopyTo(blocksStream, blockInfo.compressedSize);
                        break;
                    }
                default:
                    throw new IOException($"Unsupported compression type {compressionType}");
            }
        }
        blocksStream.Position = 0;
    }

    public int[] ParseVersion()
    {
        var versionSplit = Regex.Replace(m_Header.unityRevision, @"\D", ".").Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
        return versionSplit.Select(int.Parse).ToArray();
    }
}