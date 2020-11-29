using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    public class PackedDeserializer : IDisposable
    {
        private bool _disposed = false;

        private FileStream stream;

        public PackedDeserializer(string fileName)
        {
            this.stream = File.OpenRead(fileName);
        }

        public bool IsNextBlockAvailable()
        {
            return (this.stream.Position < this.stream.Length);
        }

        public byte[] ReadNextPackedChunk(out int origLen)
        {
            BinaryReader binaryReader = new BinaryReader(this.stream);
            Int32 chunkSize = binaryReader.ReadInt32();
            Int32 origSize = binaryReader.ReadInt32();
            origLen = origSize;

            byte[] chunk = new byte[chunkSize];
            this.stream.Read(chunk, 0, chunkSize);

            return chunk;
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
