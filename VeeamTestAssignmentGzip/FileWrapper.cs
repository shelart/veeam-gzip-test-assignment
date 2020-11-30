using System;
using System.IO;

namespace VeeamTestAssignmentGzip
{
    abstract public class FileWrapper : IDisposable
    {
        private bool _disposed = false;

        protected FileStream stream;

        public FileWrapper(string fileName) { }

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
