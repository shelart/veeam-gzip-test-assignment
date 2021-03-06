﻿using System;
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

            try
            {
                switch (command)
                {
                    case "compress":
                        {
                            FileInfo fileInfo = new FileInfo(origFileName);
                            long amountOfBlocks = fileInfo.Length.DivideRoundUp(argumentsCaptor.BlockSize);
                            List<long>[] distributedBlocksByThreads = WorkDistributor.DistributeWork(amountOfBlocks, argumentsCaptor.NumOfThreads);

                            CompressingThread[] threadObjs = new CompressingThread[distributedBlocksByThreads.Length];
                            Thread[] threads = new Thread[distributedBlocksByThreads.Length];
                            
                            if (argumentsCaptor.IsVerbose)
                            {
                                Console.WriteLine($"Blocks amount: {amountOfBlocks}");
                                Console.WriteLine("Distributed by threads:");
                            }
                            
                            for (int i = 0; i < distributedBlocksByThreads.Length; ++i)
                            {
                                if (argumentsCaptor.IsVerbose)
                                {
                                    Console.WriteLine($"Thread {i}: " + string.Join(", ", distributedBlocksByThreads[i].ToArray()));
                                }

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
                                            if (argumentsCaptor.IsVerbose)
                                            {
                                                Console.WriteLine($"Thread {i} is alive...");
                                            }
                                            ++numOfRunningThreads;
                                        }
                                        else
                                        {
                                            if (argumentsCaptor.IsVerbose)
                                            {
                                                Console.WriteLine($"Thread {i} died.");
                                            }
                                            continue;
                                        }

                                        if (argumentsCaptor.IsVerbose)
                                        {
                                            Console.WriteLine($"Thread {i}: waiting...");
                                        }
                                        threadObjs[i].WaitHandlerTowardsMainThread.WaitOne();
                                        
                                        // According to the algorithm which distributed the work
                                        // we have a guarantee here that we can sequentially write gzipped chunks.
                                        if (argumentsCaptor.IsVerbose)
                                        {
                                            Console.WriteLine($"Thread {i}: gzipped.");
                                        }
                                        fileWriter.WriteGzippedChunk(threadObjs[i].GzippedChunk, threadObjs[i].OrigChunkLength);
                                        
                                        // Let the thread to go on. We'll come to it again later when time comes.
                                        if (argumentsCaptor.IsVerbose)
                                        {
                                            Console.WriteLine($"Thread {i}: letting go.");
                                        }
                                        threadObjs[i].WaitHandlerTowardsMainThread.Reset();
                                        threadObjs[i].WaitHandlerFromMainThread.Set();
                                    }

                                    if (argumentsCaptor.IsVerbose)
                                    {
                                        Console.WriteLine($"Alive threads: {numOfRunningThreads}");
                                    }
                                } while (numOfRunningThreads > 0);
                            }
                        }
                        break;

                    case "decompress":
                        {
                            try
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
                            catch (InvalidDataException ex)
                            {
                                Console.WriteLine("Corrupted archive. Unfortunately there is probably nothing you can fix :(");
                                Console.WriteLine("Just for a case we provide tech info below:");
                                Console.WriteLine(ex);
                                return 1;
                            }
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}!");
                        PrintUsage("GZipTest.exe");
                        return 1;
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Cannot locate file: {ex.FileName}.");
                return 1;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied.");
                Console.WriteLine("Information below might help you to understand the cause:");
                Console.WriteLine(ex);
                return 1;
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error occured during reading or writing file.");
                Console.WriteLine("Information below might help you to understand the cause:");
                Console.WriteLine(ex);
                return 1;
            }

            return 0;
        }

        static void PrintUsage(string exeName)
        {
            Console.WriteLine("Usage:\n");
            Console.WriteLine($"{exeName} compress <input> <output> [/BlockSize <block>] [/MaxThreads <threads>] [/Verbose]");
            Console.WriteLine($"{exeName} decompress <input> <output>");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("    <input>\tPath to an input file.");
            Console.WriteLine("");
            Console.WriteLine("    <output>\tPath to an output file.");
            Console.WriteLine("");
            Console.WriteLine("    <block>\tSize of block (in bytes) for compressing.");
            Console.WriteLine("           \tIt must not exceed 2147483647 (2 GiB).");
            Console.WriteLine("           \tIt will be stored alongside compressed stream, you shouldn't remember it");
            Console.WriteLine("           \tnor to specify on decompressing.");
            Console.WriteLine("           \tNOTE: meaningless & ignored on decompressing (will be taken from the archive).");
            Console.WriteLine("");
            Console.WriteLine("    <threads>\tMaximum number of threads used on compressing.");
            Console.WriteLine("             \tIf /MaxThreads not set, will be chosen by number of logical cores of your machine.");
            Console.WriteLine("             \t(For this machine it is " + WorkDistributor.GetDefaultNumberOfThreads() + ".)");
            Console.WriteLine("             \tNote: if number of blocks is less than this value, number of threads");
            Console.WriteLine("             \twill be the same as the number of blocks.");
            Console.WriteLine("             \tNOTE: meaningless & ignored on decompressing (decompression is single-threaded).");
            Console.WriteLine("");
            Console.WriteLine("    /Verbose\tTurns on logging of multi-threading work distribution and threads status.");
        }
    }
}
