using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;

namespace VeeamTestAssignmentGzip
{
    public static class GzipWrapper
    {
        public static byte[] CompressBlock(byte[] data)
        {
            using (MemoryStream gzippedStream = new MemoryStream())
            {
                using (GZipStream compressionStream = new GZipStream(gzippedStream, CompressionMode.Compress))
                {
                    compressionStream.Write(data, 0, data.Length);
                    compressionStream.Flush();
                }
                return gzippedStream.ToArray();
            }
        }

        // This method handles quite complicated case of BLOCK_SIZE (aka interimBlockSize)
        // has been decreased since compressing. It might result in unpacked block size larger
        // than current BLOCK_SIZE, and this is why this method contains the logic of merging
        // unpacked blocks.
        public static byte[] DecompressBlock(byte[] gzippedBlock, int interimBlockSize)
        {
            List<byte[]> listOfUnpackedBlocks = new List<byte[]>();
            int totalUnpackedLen = 0;

            using (MemoryStream packedMemoryStream = new MemoryStream())
            {
                packedMemoryStream.Write(gzippedBlock, 0, gzippedBlock.Length);
                packedMemoryStream.Flush();
                packedMemoryStream.Seek(0, SeekOrigin.Begin);
                using (GZipStream decompressionStream = new GZipStream(packedMemoryStream, CompressionMode.Decompress))
                {
                    int unpackedBlockLen;
                    do
                    {
                        byte[] unpackedBlock = new byte[interimBlockSize];
                        unpackedBlockLen = decompressionStream.Read(unpackedBlock, 0, interimBlockSize);
                        if (unpackedBlockLen == 0)
                        {
                            // It was end of the partial stream.
                            break;
                        }

                        if (unpackedBlockLen < interimBlockSize)
                        {
                            byte[] shrunkBlock = new byte[unpackedBlockLen];
                            Buffer.BlockCopy(unpackedBlock, 0, shrunkBlock, 0, unpackedBlockLen);
                            unpackedBlock = shrunkBlock;
                        }

                        listOfUnpackedBlocks.Add(unpackedBlock);
                        totalUnpackedLen += unpackedBlockLen;
                    } while (unpackedBlockLen != 0);
                }
            }

            // Merge unpacked blocks into the single one
            // (for the complicated case of decreased BLOCK_SIZE since compressing).
            byte[] result = new byte[totalUnpackedLen];
            int copiedSoFar = 0;
            foreach (byte[] unpackedBlock in listOfUnpackedBlocks)
            {
                Buffer.BlockCopy(unpackedBlock, 0, result, copiedSoFar, unpackedBlock.Length);
                copiedSoFar += unpackedBlock.Length;
            }

            return result;
        }
    }
}
