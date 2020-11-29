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

        public byte[] ReadNextPackedChunk()
        {
            byte byte1 = readByte();
            byte byte2 = readByte();
            byte byte3 = readByte();
            byte byte4 = readByte();
            Int32ByteCoder chunkSize = new Int32ByteCoder(byte1, byte2, byte3, byte4);

            byte[] chunk = new byte[chunkSize];
            this.stream.Read(chunk, 0, chunkSize);

            return chunk;
        }

        private byte readByte()
        {
            int byt = this.stream.ReadByte();
            
            if (byt == -1)
            {
                throw new IOException("End of file");
            }

            return(byte)byt;
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
