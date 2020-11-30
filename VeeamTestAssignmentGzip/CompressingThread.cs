using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace VeeamTestAssignmentGzip
{
    public class CompressingThread : IDisposable
    {
        private bool _disposed = false;

        public AutoResetEvent WaitHandlerFromMainThread { get; private set; }
        public AutoResetEvent WaitHandlerTowardsMainThread { get; private set; }
        private UnpackedFileReader fileReader;
        private List<long> listOfBlocks;
        private readonly int blockSize;
        public byte[] GzippedChunk { get; private set; }
        public int OrigChunkLength { get; private set; }

        public CompressingThread(string origFileName, List<long> listOfBlocks, int blockSize)
        {
            this.WaitHandlerFromMainThread = new AutoResetEvent(true);
            this.WaitHandlerTowardsMainThread = new AutoResetEvent(false);
            this.fileReader = new UnpackedFileReader(origFileName);
            this.listOfBlocks = listOfBlocks;
            this.blockSize = blockSize;
        }

        public void Run()
        {
            foreach (long blockNum in this.listOfBlocks)
            {
                this.fileReader.SeekToBlock(blockNum, this.blockSize);
                byte[] origChunk = fileReader.ReadNextBlock(this.blockSize);
                this.OrigChunkLength = origChunk.Length;
                this.GzippedChunk = GzipWrapper.CompressBlock(origChunk);

                // Suspend to wait for main thread.
                this.WaitHandlerFromMainThread.Reset();
                // Notify main thread that gzippedChunk is ready for dump.
                this.WaitHandlerTowardsMainThread.Set();
                // Wait for main thread to set the handler after dumping.
                this.WaitHandlerFromMainThread.WaitOne();
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
                this.WaitHandlerFromMainThread?.Dispose();
                this.WaitHandlerTowardsMainThread?.Dispose();
                this.fileReader?.Dispose();
            }

            _disposed = true;
        }
    }
}
