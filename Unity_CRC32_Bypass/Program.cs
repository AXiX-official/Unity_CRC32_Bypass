using UnityAsset.NET.BundleFile;

namespace Unity_CRC32_Bypass;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            string FilePath = args[0];
            using FileStream FileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read);
            BundleFile BundleFile = new BundleFile(FileStream);
            Console.WriteLine($"CRC32: {BundleFile.crc32}");
        }
        else if (args.Length <= 3)
        {
            Console.WriteLine("Usage: <program> <original file path> <modified file path> <output path> <compress type[default: none]>");
            Console.WriteLine("Compress type: none, lz4, lzma, lz4hc");
            return;
        }
        else
        {
            string originalFilePath = args[0];
                    string modifiedFilePath = args[1];
                    string outputPath = args[2];
                    string compressType = args.Length > 3 ? args[3] : "none";
                    // convert to lower case
                    compressType = compressType.ToLower();
                    
                    using FileStream originalFileStream = new FileStream(originalFilePath, FileMode.Open, FileAccess.Read);
                    BundleFile originalBundleFile = new BundleFile(originalFileStream);
                    var originalCrc = originalBundleFile.crc32;
                    Console.WriteLine($"Original CRC32: {originalCrc}");
                    
                    using FileStream modifiedFileStream = new FileStream(modifiedFilePath, FileMode.Open, FileAccess.Read);
                    BundleFile modifiedBundleFile = new BundleFile(modifiedFileStream);
                    var modifiedCrc = modifiedBundleFile.crc32;
                    Console.WriteLine($"Modified CRC32: {modifiedCrc}");
                    
                    modifiedBundleFile.crc32 = originalCrc;
                    using FileStream outputFileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                    modifiedBundleFile.Write(outputFileStream, infoPacker: "lz4hc", dataPacker: compressType);
        }
    }
}