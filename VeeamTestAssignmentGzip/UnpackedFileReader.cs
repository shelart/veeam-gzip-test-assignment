using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    public class UnpackedFileReader : IDisposable
    {
        private bool _disposed = false;

        private FileStream stream;

        public UnpackedFileReader(string fileName)
        {
            this.stream = File.OpenRead(fileName);
        }

        public bool IsNextBlockAvailable()
        {
            return (this.stream.Position < this.stream.Length);
        }

        public void SeekToBlock(long blockNum, int blockSize)
        {
            this.stream.Seek(blockNum * blockSize, SeekOrigin.Begin);
        }

        public byte[] ReadNextBlock(int blockSize)
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

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                this.stream?.Dispose();
            }

            _disposed = true;
        }
    }
}
