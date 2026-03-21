using JustFileComparerCore.FileEnumerations;

namespace JustFileComparerCore.FileComparers
{
    public sealed class FileComparerWorker : FileComparerWorkerBase
    {
        #region CompareDirectoryContentAsync

        public override async Task<FileComparerWorkerResult> CompareDirectoryContentAsync(
            string sourceRoot, 
            string targetRoot, 
            FileComparisonMode fileComparisonMode,
            IProgress<FileComparisonProgress> progress,
            uint maxDegreeOfParallelism = 0, 
            uint maxWorkerCount = 0, 
            CancellationToken cancellationToken = default)
        {
            filesCount = 0;
            if (!ValidateInput(sourceRoot, targetRoot, fileComparisonMode, out FileComparerWorkerResult result))
                return result;

            result = new FileComparerWorkerResult();

            if (maxDegreeOfParallelism == 0) maxDegreeOfParallelism = (uint)Environment.ProcessorCount;

            ParallelOptions options = new ParallelOptions()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = (int)maxDegreeOfParallelism,
            };

            RaiseOnComparisonStarted();

            try
            {
                await Parallel.ForEachAsync(
                    FileEnumerator.EnumerateFilesAsyncStreamed(sourceRoot, "*", maxWorkerCount, cancellationToken),
                    options,
                    async (filePath, token) =>
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            Interlocked.Increment(ref filesCount);
                            result.SetFilesCount(filesCount);

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

            result.SetFilesCount(filesCount);

            RaiseOnComparisonCompleted();

            return result;
        }

        #endregion
    }
}
