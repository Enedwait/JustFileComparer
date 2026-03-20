using JustFileComparerCore.Extensions;
using JustFileComparerCore.FileEnumerations;

namespace JustFileComparerCore.FileComparers
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
            IProgress<FileComparisonProgress> progress = null,
            uint maxDegreeOfParallelism = 0, 
            uint maxWorkerCount = 0, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot)) return FileComparerWorkerResult.Error("Source Root should not be empty!");
            if (!Directory.Exists(sourceRoot)) return FileComparerWorkerResult.Error("Source Root does not exist!");
            if (string.IsNullOrWhiteSpace(targetRoot)) return FileComparerWorkerResult.Error("Target Root should not be empty!");
            if (!Directory.Exists(targetRoot)) return FileComparerWorkerResult.Error("Target Root does not exist!");
            if (fileComparisonMode == FileComparisonMode.None) return FileComparerWorkerResult.None;

            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            if (Path.DirectorySeparatorChar == '\\') // Windows, case insensitive
            {
                string sourceRootLower = sourceRoot.ToLowerInvariant();
                string targetRootLower = targetRoot.ToLowerInvariant();

                if (sourceRootLower == targetRootLower) return FileComparerWorkerResult.Error("Source Root and Target Root should not be the same!");
            }
            else // Unix, case-sensitive
            {
                comparison = StringComparison.Ordinal;
            }
            
            if (sourceRoot.IsSubPathOf(targetRoot, comparison)) return FileComparerWorkerResult.Error("Source Root should not be inside Target Root");
            if (targetRoot.IsSubPathOf(sourceRoot, comparison)) return FileComparerWorkerResult.Error("Target Root should not be inside Source Root");

            if (maxDegreeOfParallelism == 0) maxDegreeOfParallelism = (uint)Environment.ProcessorCount;

            ParallelOptions options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = (int)maxDegreeOfParallelism,
            };

            FileComparerWorkerResult result = new FileComparerWorkerResult();

            RaiseOnComparisonStarted();

            try
            {
                await Parallel.ForEachAsync(
                    FileEnumerator.EnumerateFilesAsync(sourceRoot, "*", maxWorkerCount, cancellationToken),
                    options,
                    async (filePath, token) =>
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            FileComparison comparison = await ProcessFile(filePath, sourceRoot, targetRoot, fileComparisonMode, token);
                            if (!token.IsCancellationRequested)
                            {
                                result.Add(comparison);

                                progress?.Report(new FileComparisonProgress()
                                {
                                    SuccessfulComparisonsCount = result.SuccessfulComparisonsCount,
                                    FailedComparisonsCount = result.FailedComparisonsCount,
                                    CurrentComparison = comparison,
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                result.SetCanceled();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            if (cancellationToken.IsCancellationRequested)
                result.SetCanceled();

            RaiseOnComparisonCompleted();

            return result;
        }

        #endregion

        #region Single File Processing

        private async Task<FileComparison> ProcessFile(string filePath, string sourceRoot, string targetRoot, FileComparisonMode comparisonMode, CancellationToken cancellationToken)
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

        private void RaiseOnComparisonStarted() => OnComparisonStarted?.Invoke(this, EventArgs.Empty);
        private void RaiseOnComparisonCompleted() => OnComparisonCompleted?.Invoke(this, EventArgs.Empty);
    }
}
