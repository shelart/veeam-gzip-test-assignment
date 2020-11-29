using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    public class PackedSerializer : IDisposable
    {
        private bool _disposed = false;

        private FileStream stream;

        public PackedSerializer(string fileName)
        {
            this.stream = File.Create(fileName);
        }

        public void WriteGzippedChunk(byte[] gzippedChunk, Int32 origLen)
        {
            int packedLen = gzippedChunk.Length;
            BinaryWriter binaryWriter = new BinaryWriter(this.stream);
            binaryWriter.Write((Int32)packedLen);
            binaryWriter.Write(origLen);

            this.stream.Write(gzippedChunk, 0, packedLen);
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
                this.stream?.Flush();
                this.stream?.Dispose();
            }

            _disposed = true;
        }
    }
}
