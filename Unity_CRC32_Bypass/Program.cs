using UnityAsset.NET.Enums;
using UnityAsset.NET.Extensions;
using UnityAsset.NET.Files;

namespace Unity_CRC32_Bypass;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            BundleFile BundleFile = new BundleFile(args[0]);
            Console.WriteLine($"CRC32: {BundleFile.CalculateCrc32()}");
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
            var compressionType = compressType.ToLower() switch
            {
                "none" => CompressionType.None,
                "lz4" => CompressionType.Lz4,
                "lz4hc" => CompressionType.Lz4HC,
                "lzma" => CompressionType.Lzma,
                _ => throw new Exception($"Unsupported Compress type: {compressType}. ")
            };
            
            BundleFile originalBundleFile = new BundleFile(originalFilePath);
            var originalCrc = originalBundleFile.CalculateCrc32();
            Console.WriteLine($"Original CRC32: {originalCrc}");
            
            BundleFile modifiedBundleFile = new BundleFile(modifiedFilePath);
            var modifiedCrc = modifiedBundleFile.CalculateCrc32();
            Console.WriteLine($"Modified CRC32: {modifiedCrc}");
            
            modifiedBundleFile.PatchCrc32(modifiedCrc);
            modifiedBundleFile.Serialize(outputPath, infoPacker: CompressionType.Lz4HC, dataPacker: compressionType);
        }
    }
}