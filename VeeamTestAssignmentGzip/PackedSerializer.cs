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
            Int32ByteCoder codedPackedLen = packedLen;
            this.stream.WriteByte(codedPackedLen.Byte1);
            this.stream.WriteByte(codedPackedLen.Byte2);
            this.stream.WriteByte(codedPackedLen.Byte3);
            this.stream.WriteByte(codedPackedLen.Byte4);

            Int32ByteCoder codedOrigLen = origLen;
            this.stream.WriteByte(codedOrigLen.Byte1);
            this.stream.WriteByte(codedOrigLen.Byte2);
            this.stream.WriteByte(codedOrigLen.Byte3);
            this.stream.WriteByte(codedOrigLen.Byte4);

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
