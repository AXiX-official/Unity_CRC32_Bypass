using System.Diagnostics.CodeAnalysis;
using UnityAsset.NET;
using UnityAsset.NET.BundleFiles;
using UnityAsset.NET.Extensions;
using UnityAsset.NET.FileSystem;
using UnityAsset.NET.FileSystem.DirectFileSystem;

namespace Unity_CRC32_Bypass;

public class Program
{
    static bool TryGetBundleFromPath(string path, [NotNullWhen(returnValue:true)]out BundleFile? bundleFile)
    {
        bundleFile = null;
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return false;
        }
        using var file = new DirectFileInfo(path);
        if (file.FileType != FileType.BundleFile)
        {
            Console.WriteLine($"{path} may not a valid BundleFile!");
            return false;
        }
        bundleFile = new BundleFile(file);
        return true;
    }
    
    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            if (!TryGetBundleFromPath(args[0], out var bundleFile)) return;
            Console.WriteLine($"CRC32: {bundleFile.CalculateCrc32()}");
        }
        else if (args.Length < 3 || args.Length > 4)
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
            
            if (!TryGetBundleFromPath(originalFilePath, out var originalBundleFile)) return;
            var originalCrc = originalBundleFile.CalculateCrc32();
            Console.WriteLine($"Original CRC32: {originalCrc}");
            
            if (!TryGetBundleFromPath(modifiedFilePath, out var modifiedBundleFile)) return;
            var modifiedCrc = modifiedBundleFile.CalculateCrc32();
            Console.WriteLine($"Modified CRC32: {modifiedCrc}");
            
            modifiedBundleFile.PatchCrc32(originalCrc);
            modifiedBundleFile.Serialize(outputPath, CompressionType.Lz4HC, compressionType, null);
        }
    }
}