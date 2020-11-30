using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    public class PackedSerializer : FileWrapper
    {
        private bool _disposed = false;

        public PackedSerializer(string fileName)
            : base(fileName)
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

        override protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                this.stream?.Flush();
            }

            base.Dispose(disposing);

            _disposed = true;
        }
    }
}
