using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace VeeamTestAssignmentGzip
{
    class Program
    {
        static int Main(string[] args)
        {
            ArgumentsCaptor argumentsCaptor;
            try
            {
                argumentsCaptor = new ArgumentsCaptor(args);
            } catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException);
                }
                Console.WriteLine("");
                PrintUsage("GZipTest.exe");
                return 1;
            }

            string command      = argumentsCaptor.Command;
            string origFileName = argumentsCaptor.OrigFileName;
            string resFileName  = argumentsCaptor.ResFileName;

            switch (command)
            {
                case "compress":
                    {
                        FileInfo fileInfo = new FileInfo(origFileName);
                        long amountOfBlocks = fileInfo.Length.DivideRoundUp(argumentsCaptor.BlockSize);
                        List<long>[] distributedBlocksByThreads = WorkDistributor.DistributeWork(amountOfBlocks, argumentsCaptor.NumOfThreads);

                        CompressingThread[] threadObjs = new CompressingThread[distributedBlocksByThreads.Length];
                        Thread[] threads = new Thread[distributedBlocksByThreads.Length];
                        Console.WriteLine($"Blocks amount: {amountOfBlocks}");
                        Console.WriteLine("Distributed by threads:");
                        for (int i = 0; i < distributedBlocksByThreads.Length; ++i)
                        {
                            Console.WriteLine($"Thread {i}: " + string.Join(", ", distributedBlocksByThreads[i].ToArray()));
                            threadObjs[i] = new CompressingThread(origFileName, distributedBlocksByThreads[i], argumentsCaptor.BlockSize);
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
            Console.WriteLine($"{exeName} compress <input> <output> /BlockSize <block> /MaxThreads <threads>");
            Console.WriteLine($"{exeName} decompress <input> <output>");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("    <input>\tPath to an input file.");
            Console.WriteLine("");
            Console.WriteLine("    <output>\tPath to an output file.");
            Console.WriteLine("");
            Console.WriteLine("    <block>\tSize of block (in bytes) for compressing.");
            Console.WriteLine("           \tIt must not exceed 4294967296 (4 GiB).");
            Console.WriteLine("           \tIt will be stored alongside compressed stream, you shouldn't remember it");
            Console.WriteLine("           \tnor to specify on decompressing.");
            Console.WriteLine("           \tNOTE: meaningless & ignored on decompressing (will be taken from the archive).");
            Console.WriteLine("");
            Console.WriteLine("    <threads>\tMaximum number of threads used on compressing.");
            Console.WriteLine("             \tIf not set, will be chosen by number of logical cores of your machine.");
            Console.WriteLine("             \t(For this machine it is " + WorkDistributor.GetDefaultNumberOfThreads() + ".)");
            Console.WriteLine("             \tNote: if number of blocks is less than this value, number of threads");
            Console.WriteLine("             \twill be the same as the number of blocks.");
            Console.WriteLine("             \tNOTE: meaningless & ignored on decompressing (decompression is single-threaded).");
        }
    }
}
