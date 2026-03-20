using System.Collections.Concurrent;

namespace JustFileComparerCore.FileComparers
{
    public sealed class FileComparerWorkerResult
    {
        #region Fields

        private ulong successfulComparisonsCount = 0;
        private ulong failedComparisonsCount = 0;

        #endregion

        #region Properties

        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; }

        public ulong SuccessfulComparisonsCount => successfulComparisonsCount;
        public ulong FailedComparisonsCount => failedComparisonsCount;

        public ConcurrentBag<FileComparison> FailedComparisons { get; private set; } = new ConcurrentBag<FileComparison>();

        #endregion

        #region Init

        public FileComparerWorkerResult()
        {
            Success = true;
            ErrorMessage = String.Empty;
        }

        #endregion

        #region Methods

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

        public void SetCanceled()
        {
            Success = false;
            ErrorMessage = "Canceled";
        }

        public override string ToString()
        {
            if (Success) return $"S: {SuccessfulComparisonsCount}, F: {FailedComparisonsCount}";
            return $"{ErrorMessage}";
        }

        #endregion

        #region Static

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

        #endregion
    }
}
