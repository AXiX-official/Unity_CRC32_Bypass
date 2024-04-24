namespace Unity_CRC32_Bypass;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: <program> <original file path> <modified file path> <output path>");
            return;
        }

        string originalFilePath = args[0];
        string modifiedFilePath = args[1];
        string outputPath = args[2];
        
        BundleFile originalBundleFile = new BundleFile(originalFilePath);
        uint originalCRC = originalBundleFile.crc32;
        Console.WriteLine("Original CRC32: 0x{0:X8}", originalCRC);
        
        BundleFile modifiedBundleFile = new BundleFile(modifiedFilePath);
        uint modifiedCRC = modifiedBundleFile.crc32; 
        Console.WriteLine("Modified CRC32: 0x{0:X8}", modifiedCRC);
        
        uint append = CRC32.rCRC(originalCRC, modifiedCRC);
        modifiedBundleFile.append(append);
        uint newCRC = modifiedBundleFile.crc32;
        if (newCRC != originalCRC)
        {
            throw new Exception("Fix CRC failed!CRC32 mismatch!");
        }
        Console.WriteLine("CRC32 fixed successfully!");
        Console.WriteLine("New CRC32: 0x{0:X8}", newCRC);
        modifiedBundleFile.WriteToFile(outputPath);
        Console.WriteLine("Output file saved to: {0}", outputPath);
    }
}