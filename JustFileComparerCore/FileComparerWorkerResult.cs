using System.Collections.Concurrent;

namespace JustFileComparerCore
{
    public sealed class FileComparerWorkerResult
    {
        private ulong successfulComparisonsCount = 0;
        private ulong failedComparisonsCount = 0;

        public bool Success { get; private set; }

        public ulong SuccessfulComparisonsCount => successfulComparisonsCount;
        public ulong FailedComparisonsCount => failedComparisonsCount;

        public ConcurrentBag<FileComparison> FailedComparisons { get; private set; } = new ConcurrentBag<FileComparison>();

        public string ErrorMessage { get; private set; }

        public FileComparerWorkerResult()
        {
            Success = true;
        }

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

        public override string ToString()
        {
            if (Success) return $"S: {SuccessfulComparisonsCount}, F: {FailedComparisonsCount}";
            return $"{ErrorMessage}";
        }

        public static FileComparerWorkerResult None => new FileComparerWorkerResult()
        {
            Success = false,
            FailedComparisons = null
        };

        public static FileComparerWorkerResult Error(string errorMessage) => new FileComparerWorkerResult()
        {
            Success = false,
            ErrorMessage = errorMessage,
            FailedComparisons = null,
        };
    }
}
