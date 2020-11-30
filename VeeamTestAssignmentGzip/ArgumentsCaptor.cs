using System;

namespace VeeamTestAssignmentGzip
{
    public class ArgumentsCaptor
    {
        public string Command { get; private set; }
        public string OrigFileName { get; private set; }
        public string ResFileName { get; private set; }
        public Int32 BlockSize { get; private set; }
        public int NumOfThreads { get; private set; }
        public bool IsVerbose { get; private set; }

        public ArgumentsCaptor(string[] args)
        {
            if (args.Length < 3)
            {
                throw new ArgumentException("Not enough arguments provided.");
            }

            Command = args[0];
            OrigFileName = args[1];
            ResFileName = args[2];

            // Defaults.
            BlockSize = 1024 * 1024;
            NumOfThreads = WorkDistributor.GetDefaultNumberOfThreads();

            for (int i = 3; i < args.Length; ++i)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "/blocksize":
                        BlockSize = readNextInt(args, ref i);
                        break;

                    case "/maxthreads":
                        NumOfThreads = readNextInt(args, ref i);
                        break;

                    case "/verbose":
                        IsVerbose = true;
                        break;

                    default:
                        throw new ArgumentException($"Unknown argument: {args[i]}");
                }
            }
        }

        private int readNextInt(string[] args, ref int curIdx)
        {
            if (curIdx == args.Length - 1)
            {
                throw new ArgumentException($"{args[curIdx]} must be followed by a number.");
            }

            int paramValue;
            try
            {
                paramValue = int.Parse(args[curIdx + 1]);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"{args[curIdx]} value cannot be parsed: {args[curIdx + 1]} due to: ", ex);
            }

            if (paramValue < 0)
            {
                throw new ArgumentException($"{args[curIdx]} value must be positive.");
            }

            ++curIdx; // skip the parsed next arg (value)
            return paramValue;
        }
    }
}
