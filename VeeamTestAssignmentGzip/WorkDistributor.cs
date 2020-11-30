using System;
using System.Collections.Generic;

namespace VeeamTestAssignmentGzip
{
    public static class WorkDistributor
    {
        public static int GetDefaultNumberOfThreads() => Environment.ProcessorCount;

        public static List<long>[] DistributeWork(long amountOfBlocks, int numOfThreads)
        {
            List<long>[] result = new List<long>[Math.Min(amountOfBlocks, numOfThreads)];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = new List<long>();
                for (long j = i; j < amountOfBlocks; j += numOfThreads)
                {
                    result[i].Add(j);
                }
            }

            return result;
        }
    }
}
