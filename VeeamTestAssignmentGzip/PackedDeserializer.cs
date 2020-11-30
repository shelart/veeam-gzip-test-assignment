using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    public class PackedDeserializer : FileReader
    {
        public PackedDeserializer(string fileName)
            : base(fileName)
        { }

        public byte[] ReadNextPackedChunk(out int origLen)
        {
            BinaryReader binaryReader = new BinaryReader(this.stream);
            Int32 chunkSize = binaryReader.ReadInt32();
            Int32 origSize = binaryReader.ReadInt32();
            origLen = origSize;

            if ((chunkSize < 0) || (origSize < 0))
            {
                throw new InvalidDataException("Met negative block size.");
            }

            byte[] chunk = new byte[chunkSize];
            this.stream.Read(chunk, 0, chunkSize);

            return chunk;
        }
    }
}
