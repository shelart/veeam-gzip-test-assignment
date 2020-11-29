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

        public static byte[] DecompressBlock(byte[] gzippedBlock, int expectedLength)
        {
            using (MemoryStream packedMemoryStream = new MemoryStream())
            {
                packedMemoryStream.Write(gzippedBlock, 0, gzippedBlock.Length);
                packedMemoryStream.Flush();
                packedMemoryStream.Seek(0, SeekOrigin.Begin);
                using (GZipStream decompressionStream = new GZipStream(packedMemoryStream, CompressionMode.Decompress))
                {
                    byte[] unpackedBlock = new byte[expectedLength];
                    int unpackedBlockLen = decompressionStream.Read(unpackedBlock, 0, expectedLength);

                    if (unpackedBlockLen < expectedLength)
                    {
                        byte[] shrunkBlock = new byte[unpackedBlockLen];
                        Buffer.BlockCopy(unpackedBlock, 0, shrunkBlock, 0, unpackedBlockLen);
                        unpackedBlock = shrunkBlock;
                    }

                    return unpackedBlock;
                }
            }
        }
    }
}
