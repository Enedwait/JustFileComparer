using JustFileComparerCore.Extensions;

namespace JustFileComparerCore.FileComparers
{
    public interface IFileComparerWorker
    {
        public event EventHandler OnComparisonStarted;
        public event EventHandler OnComparisonCompleted;
    }

    public abstract class FileComparerWorkerBase : IFileComparerWorker
    {
        public event EventHandler OnComparisonStarted;
        public event EventHandler OnComparisonCompleted;

        protected ulong _filesCount;

        public ulong FilesCount => _filesCount;

        public abstract Task<FileComparerWorkerResult> CompareDirectoryContentAsync(
            string sourceRoot,
            string targetRoot,
            FileComparisonMode fileComparisonMode = FileComparisonMode.Size | FileComparisonMode.Hash,
            IProgress<FileComparisonProgress> progress = null,
            uint maxDegreeOfParallelism = 0,
            uint maxWorkerCount = 0,
            CancellationToken cancellationToken = default);

        #region ValidateInput

        protected virtual bool ValidateInput(string sourceRoot, string targetRoot, FileComparisonMode fileComparisonMode, out FileComparerWorkerResult error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(sourceRoot)) { error = FileComparerWorkerResult.Error("Source Root should not be empty!"); return false; }
            if (!Directory.Exists(sourceRoot)) { error = FileComparerWorkerResult.Error("Source Root does not exist!"); return false; }
            if (string.IsNullOrWhiteSpace(targetRoot)) { error = FileComparerWorkerResult.Error("Target Root should not be empty!"); return false; }
            if (!Directory.Exists(targetRoot)) { error = FileComparerWorkerResult.Error("Target Root does not exist!"); return false; }
            if (fileComparisonMode == FileComparisonMode.None) { error = FileComparerWorkerResult.None; return false; }

            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            if (Path.DirectorySeparatorChar == '\\') // Windows, case insensitive
            {
                string sourceRootLower = sourceRoot.ToLowerInvariant();
                string targetRootLower = targetRoot.ToLowerInvariant();

                if (sourceRootLower == targetRootLower) { error = FileComparerWorkerResult.Error("Source Root and Target Root should not be the same!"); return false; }
            }
            else // Unix, case-sensitive
            {
                comparison = StringComparison.Ordinal;
            }

            if (sourceRoot.IsSubPathOf(targetRoot, comparison)) { error = FileComparerWorkerResult.Error("Source Root should not be inside Target Root"); return false; }
            if (targetRoot.IsSubPathOf(sourceRoot, comparison)) { error = FileComparerWorkerResult.Error("Target Root should not be inside Source Root"); return false; }

            return true;
        }

        #endregion

        #region Single File Processing

        protected virtual async Task<FileComparison> ProcessFile(string filePath, string sourceRoot, string targetRoot, FileComparisonMode comparisonMode, CancellationToken cancellationToken)
        {
            FileComparison comparison = new FileComparison()
            {
                Source = filePath,
                Target = filePath.Replace(sourceRoot, targetRoot),
                Result = FileComparisonResult.None,
                Mode = comparisonMode
            };

            if (!File.Exists(comparison.Source))
            {
                comparison.Result = FileComparisonResult.SourceFileDoesNotExist;
                return comparison;
            }

            if (!File.Exists(comparison.Target))
            {
                comparison.Result = FileComparisonResult.TargetFileDoesNotExist;
                return comparison;
            }

            bool result = await FileComparer.AreFilesEqualAsync(comparison.Source, comparison.Target, comparisonMode, cancellationToken);
            comparison.Result = result ? FileComparisonResult.Equal : FileComparisonResult.Differ;
            return comparison;
        }

        #endregion

        protected void RaiseOnComparisonStarted() => OnComparisonStarted?.Invoke(this, EventArgs.Empty);
        protected void RaiseOnComparisonCompleted() => OnComparisonCompleted?.Invoke(this, EventArgs.Empty);
    }
}
