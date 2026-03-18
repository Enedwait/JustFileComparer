namespace JustFileComparerCore
{
    public struct FileComparison
    {
        public string Source;
        public string Target;
        public FileComparisonResult Result;
        public FileComparisonMode Mode;

        public override string ToString() => $"{Source} vs {Target}: {Result}, {Mode}";
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
