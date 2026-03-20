namespace JustFileComparerCore.FileComparers
{
    public struct FileComparison
    {
        public string Source;
        public string Target;
        public FileComparisonResult Result;
        public FileComparisonMode Mode;

        public override string ToString() => $"'{Source}' vs '{Target}': {Result}, {Mode}";
    }

    public struct FileComparisonProgress
    {
        public FileComparison CurrentComparison;
        public ulong SuccessfulComparisonsCount;
        public ulong FailedComparisonsCount;

        public ulong TotalComparisonsCount => SuccessfulComparisonsCount + FailedComparisonsCount;

        public override string ToString() => $"Success:{SuccessfulComparisonsCount} Failure:{FailedComparisonsCount} Total:{TotalComparisonsCount}; Current Comparison: {CurrentComparison}";
    }

    public enum FileComparisonResult
    {
        None = 0,
        Equal,
        Differ,
        SourceFileDoesNotExist,
        TargetFileDoesNotExist
    }

    [Flags]
    public enum FileComparisonMode
    {
        None = 0,
        Size = 1,
        Hash = 2,
        Bytes = 4
    }
}
