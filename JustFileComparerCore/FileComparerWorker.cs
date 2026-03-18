using System.Collections.Concurrent;

namespace JustFileComparerCore
{
    public sealed class FileComparerWorker
    {
        public event EventHandler OnComparisonStarted;
        public event EventHandler OnComparisonCompleted;

        #region CompareDirectoryContentAsync

        public async Task<FileComparerWorkerResult> CompareDirectoryContentAsync(
            string sourceRoot, 
            string targetRoot, 
            FileComparisonMode fileComparisonMode = FileComparisonMode.Size | FileComparisonMode.Hash,
            IProgress<FileComparison> progress = null,
            uint maxDegreeOfParallelism = 0, 
            uint maxWorkerCount = 0, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sourceRoot) || !Directory.Exists(sourceRoot)) return FileComparerWorkerResult.None;
            if (string.IsNullOrEmpty(targetRoot) || !Directory.Exists(sourceRoot)) return FileComparerWorkerResult.None;
            if (fileComparisonMode == FileComparisonMode.None) return FileComparerWorkerResult.None;

            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            if (Path.DirectorySeparatorChar == '\\') // Windows, case insensitive
            {
                string sourceRootLower = sourceRoot.ToLowerInvariant();
                string targetRootLower = targetRoot.ToLowerInvariant();

                if (sourceRootLower == targetRootLower) return FileComparerWorkerResult.None;
            }
            else // Unix, case-sensitive
            {
                comparison = StringComparison.Ordinal;
            }
            
            if (sourceRoot.IsInside(targetRoot, comparison)) return FileComparerWorkerResult.None;
            if (targetRoot.IsInside(sourceRoot, comparison)) return FileComparerWorkerResult.None;

            if (maxDegreeOfParallelism == 0) maxDegreeOfParallelism = (uint)Environment.ProcessorCount;

            ParallelOptions options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = (int)maxDegreeOfParallelism,
            };

            FileComparerWorkerResult result = new FileComparerWorkerResult();

            RaiseOnComparisonStarted();

            await Parallel.ForEachAsync(
                FileEnumerator.EnumerateFilesAsync(sourceRoot, maxWorkerCount: maxWorkerCount, cancellationToken: cancellationToken),
                options,
                async (filePath, token) =>
                {
                    FileComparison comparison = await ProcessFile(filePath, sourceRoot, targetRoot, fileComparisonMode, token);
                    result.Add(comparison);
                    progress?.Report(comparison);
                });

            RaiseOnComparisonCompleted();

            return result;
        }

        #endregion

        #region Single File Processing

        private async Task<FileComparison> ProcessFile(string filePath, string sourceRoot, string targetRoot, FileComparisonMode comparisonMode, CancellationToken token)
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

            bool result = await FileComparer.AreFilesEqualAsync(comparison.Source, comparison.Target, comparisonMode);
            comparison.Result = result ? FileComparisonResult.Equal : FileComparisonResult.Differ;
            return comparison;
        }

        #endregion

        private void RaiseOnComparisonStarted() => OnComparisonStarted?.Invoke(this, EventArgs.Empty);
        private void RaiseOnComparisonCompleted() => OnComparisonCompleted?.Invoke(this, EventArgs.Empty);
    }
}
