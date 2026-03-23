using System.Collections.Concurrent;
using JustFileComparerCore.FileEnumerations;

namespace JustFileComparerCore.FileComparers
{
    public class SequentialFileComparerWorker : FileComparerWorkerBase
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
            
            ConcurrentBag<string> files = new ConcurrentBag<string>();
            await foreach (string file in FileEnumerator.EnumerateFilesAsyncStreamed(sourceRoot, "*", maxWorkerCount, cancellationToken))
            {
                Interlocked.Increment(ref filesCount);
                files.Add(file);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Parallel.ForEachAsync(files,
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
#if DEBUG
                                Console.WriteLine(ex);
#endif
                            }
                        });
                }
                catch (OperationCanceledException)
                {
                    result.SetCanceled();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex);
#endif
                }
            }

            if (cancellationToken.IsCancellationRequested)
                result.SetCanceled();

            RaiseOnComparisonCompleted();

            return result;
        }

        #endregion
    }
}
