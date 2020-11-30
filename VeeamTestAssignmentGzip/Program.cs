using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace VeeamTestAssignmentGzip
{
    class Program
    {
        private static Int32 BLOCK_SIZE = 1024 * 1024;

        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                PrintUsage("GZipTest.exe");
                return 1;
            }

            string command      = args[0];
            string origFileName = args[1];
            string resFileName  = args[2];

            switch (command)
            {
                case "compress":
                    {
                        FileInfo fileInfo = new FileInfo(origFileName);
                        long amountOfBlocks = fileInfo.Length.DivideRoundUp(BLOCK_SIZE);
                        int numOfThreads = WorkDistributor.GetDefaultNumberOfThreads();
                        List<long>[] distributedBlocksByThreads = WorkDistributor.DistributeWork(amountOfBlocks, numOfThreads);

                        CompressingThread[] threadObjs = new CompressingThread[distributedBlocksByThreads.Length];
                        Thread[] threads = new Thread[distributedBlocksByThreads.Length];
                        Console.WriteLine($"Blocks amount: {amountOfBlocks}");
                        Console.WriteLine("Distributed by threads:");
                        for (int i = 0; i < distributedBlocksByThreads.Length; ++i)
                        {
                            Console.WriteLine($"Thread {i}: " + string.Join(", ", distributedBlocksByThreads[i].ToArray()));
                            threadObjs[i] = new CompressingThread(origFileName, distributedBlocksByThreads[i], BLOCK_SIZE);
                            threads[i] = new Thread(new ThreadStart(threadObjs[i].Run));
                        }

                        using (PackedSerializer fileWriter = new PackedSerializer(resFileName))
                        {
                            // Start all threads.
                            for (int i = 0; i < threads.Length; ++i)
                            {
                                threads[i].Start();
                            }

                            int numOfRunningThreads;
                            do
                            {
                                numOfRunningThreads = 0;
                                // Wait for all threads to prepare gzipped chunks.
                                for (int i = 0; i < threads.Length; ++i)
                                {
                                    if (threads[i].IsAlive)
                                    {
                                        Console.WriteLine($"Thread {i} is alive...");
                                        ++numOfRunningThreads;
                                    } else
                                    {
                                        Console.WriteLine($"Thread {i} died.");
                                        continue;
                                    }

                                    Console.WriteLine($"Thread {i}: waiting...");
                                    threadObjs[i].WaitHandlerTowardsMainThread.WaitOne();
                                    // According to the algorithm which distributed the work
                                    // we have a guarantee here that we can sequentially write gzipped chunks.
                                    Console.WriteLine($"Thread {i}: gzipped.");
                                    fileWriter.WriteGzippedChunk(threadObjs[i].GzippedChunk, threadObjs[i].OrigChunkLength);
                                    // Let the thread to go on. We'll come to it again later when time comes.
                                    Console.WriteLine($"Thread {i}: letting go.");
                                    threadObjs[i].WaitHandlerTowardsMainThread.Reset();
                                    threadObjs[i].WaitHandlerFromMainThread.Set();
                                }
                                Console.WriteLine($"Alive threads: {numOfRunningThreads}");
                            } while (numOfRunningThreads > 0);
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
            Console.WriteLine("");
            Console.WriteLine("    <input>\tPath to an input file.");
            Console.WriteLine("    <output>\tPath to an output file.");
        }
    }
}
