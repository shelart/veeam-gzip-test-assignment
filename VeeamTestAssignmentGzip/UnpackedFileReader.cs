using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    public class UnpackedFileReader : FileReader
    {
        private readonly int blockSize;

        public UnpackedFileReader(string fileName, int blockSize)
            : base(fileName)
        {
            this.blockSize = blockSize;
        }

        public void SeekToBlock(long blockNum)
        {
            this.stream.Seek(blockNum * blockSize, SeekOrigin.Begin);
        }

        public byte[] ReadNextBlock()
        {
            byte[] chunk = new byte[blockSize];
            int chunkSize = this.stream.Read(chunk, 0, blockSize);
            if (chunkSize < blockSize)
            {
                byte[] shrunkChunk = new byte[chunkSize];
                Buffer.BlockCopy(chunk, 0, shrunkChunk, 0, chunkSize);
                return shrunkChunk;
            } else
            {
                return chunk;
            }
        }
    }
}
