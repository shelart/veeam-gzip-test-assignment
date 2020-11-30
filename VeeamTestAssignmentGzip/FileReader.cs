using System.IO;

namespace VeeamTestAssignmentGzip
{
    abstract public class FileReader : FileWrapper
    {
        public FileReader(string fileName)
            : base(fileName)
        {
            this.stream = File.OpenRead(fileName);
        }

        public bool IsNextBlockAvailable() => (this.stream.Position < this.stream.Length);
    }
}
