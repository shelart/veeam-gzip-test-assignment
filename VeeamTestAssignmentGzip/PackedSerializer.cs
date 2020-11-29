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

        public void WriteGzippedChunk(byte[] gzippedChunk)
        {
            int len = gzippedChunk.Length;
            Int32ByteCoder codedLen = len;
            this.stream.WriteByte(codedLen.Byte1);
            this.stream.WriteByte(codedLen.Byte2);
            this.stream.WriteByte(codedLen.Byte3);
            this.stream.WriteByte(codedLen.Byte4);

            this.stream.Write(gzippedChunk, 0, len);
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
