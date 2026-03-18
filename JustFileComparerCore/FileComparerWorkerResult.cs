using System.Collections.Concurrent;

namespace JustFileComparerCore
{
    public sealed class FileComparerWorkerResult
    {
        private ulong successfulComparisonsCount = 0;
        private ulong failedComparisonsCount = 0;

        public ulong SuccessfulComparisonsCount => successfulComparisonsCount;
        public ulong FailedComparisonsCount => failedComparisonsCount;

        public ConcurrentBag<FileComparison> FailedComparisons { get; private set; } = new ConcurrentBag<FileComparison>();

        public void Add(FileComparison comparison)
        {
            if (comparison.Result != FileComparisonResult.Equal)
            {
                Interlocked.Increment(ref failedComparisonsCount);
                FailedComparisons.Add(comparison);
            }
            else
            {
                Interlocked.Increment(ref successfulComparisonsCount);
            }
        }

        public static FileComparerWorkerResult None => new FileComparerWorkerResult() { FailedComparisons = null };
    }
}
