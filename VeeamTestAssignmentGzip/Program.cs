using System;
using System.IO;
using System.IO.Compression;

namespace VeeamTestAssignmentGzip
{
    class Program
    {
        private static Int32 BLOCK_SIZE = 1024 * 1024;

        static int Main(string[] args)
        {
            string command      = args[0];
            string origFileName = args[1];
            string resFileName  = args[2];

            switch (command)
            {
                case "compress":
                    {
                        using (UnpackedFileReader fileReader = new UnpackedFileReader(origFileName))
                        using (PackedSerializer fileWriter = new PackedSerializer(resFileName))
                        {
                            while (fileReader.IsNextBlockAvailable())
                            {
                                byte[] origChunk = fileReader.ReadNextBlock(BLOCK_SIZE);
                                byte[] gzippedChunk = GzipWrapper.CompressBlock(origChunk);
                                fileWriter.WriteGzippedChunk(gzippedChunk, origChunk.Length);
                            }
                        }
                    }
                    break;

                case "decompress":
                    {
                        using (PackedDeserializer fileReader = new PackedDeserializer(origFileName))
                        using (FileStream fileWriter = File.Create(resFileName))
                        {
                            while (fileReader.IsNextBlockAvailable())
                            {
                                byte[] packedChunk = fileReader.ReadNextPackedChunk(out int origLength);
                                byte[] unpackedChunk = GzipWrapper.DecompressBlock(packedChunk, origLength);
                                fileWriter.Write(unpackedChunk, 0, unpackedChunk.Length);
                            }
                            fileWriter.Flush();
                        }
                    }
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}!");
                    PrintUsage("GZipTest.exe");
                    return 1;
            }

            return 0;
        }

        static void PrintUsage(string exeName)
        {
            Console.WriteLine("Usage:\n");
            Console.WriteLine($"{exeName} compress <input> <output>");
            Console.WriteLine($"{exeName} decompress <input> <output>");
            Console.WriteLine("\n");
            Console.WriteLine("    <input>\tPath to an input file.");
            Console.WriteLine("    <output>\tPath to an output file.");
        }
    }
}
