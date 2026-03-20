using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace JustFileComparerCore.FileEnumerations
{
    public static class FileEnumerator
    {
        #region EnumerateFiles

        /// <summary>
        /// Enumerates all files in the specified root directory and its subdirectories that match a search pattern using multiple parallel workers.
        /// </summary>
        /// <param name="root">the root directory path.</param>
        /// <param name="searchPattern">the file name pattern for search.</param>
        /// <param name="maxWorkerCount">the max number of concurrent workers; if set to 0 (default) then the number of logical processors is used.</param> 
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>The <see cref="IEnumerable{String}"/> containing the full paths of all files found as requested.</returns>
        public static IEnumerable<string> EnumerateFiles(string root, string searchPattern = "*", uint maxWorkerCount = 0, IProgress<string> progress = default, CancellationToken cancellationToken = default)
        {
            var files = new ConcurrentBag<string>();
            var directories = new ConcurrentQueue<string>();

            directories.Enqueue(root);

            using (CountdownEvent countdown = new CountdownEvent(1))
            {
                int workerCount = maxWorkerCount == 0 ? Environment.ProcessorCount : (int)maxWorkerCount;
                Task[] workers = new Task[workerCount];

                for (int i = 0; i < workerCount; i++)
                {
                    workers[i] = Task.Run(() =>
                    {
                        while (true)
                        {
                            if (!directories.TryDequeue(out string currentDirectory))
                            {
                                Thread.Sleep(10);
                                continue;
                            }

                            try
                            {
                                if (cancellationToken.IsCancellationRequested) break;

                                foreach (var file in Directory.EnumerateFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly))
                                {
                                    if (cancellationToken.IsCancellationRequested) break;

                                    files.Add(file);
                                    progress?.Report(file);
                                }

                                if (cancellationToken.IsCancellationRequested) break;

                                foreach (var directory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                                {
                                    if (cancellationToken.IsCancellationRequested) break;

                                    directories.Enqueue(directory);
                                    countdown.AddCount();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                            finally
                            {
                                try
                                {
                                    countdown.Signal();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                            }

                            if (cancellationToken.IsCancellationRequested) break;
                        }
                    });
                }

                try
                {
                    countdown.Wait(cancellationToken);
                }
                catch(OperationCanceledException)
                {}
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return files;
        }

        #endregion

        #region EnumerateFilesAsync

        /// <summary>
        /// Asynchronously enumerates all files in the specified root directory and its subdirectories that match a search pattern using multiple parallel workers and streaming search results as they are discovered.
        /// </summary>
        /// <param name="root">the root directory path.</param>
        /// <param name="searchPattern">the file name pattern for search.</param>
        /// <param name="maxWorkerCount">the max number of concurrent workers; if set to 0 (default) then the number of logical processors is used.</param>
        /// <param name="cancellationToken">the <see cref="CancellationToken"/> to observe enumeration to complete or to cancel.</param>
        /// <returns>The <see cref="IAsyncEnumerable{String}"/> containing the full paths of all files found as requested.</returns>
        public static async IAsyncEnumerable<string> EnumerateFilesAsAsyncEnumerable(string root, string searchPattern = "*", uint maxWorkerCount = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Channel<string> directoryChannel = Channel.CreateUnbounded<string>();
            Channel<string> fileChannel = Channel.CreateUnbounded<string>();

            int pendingDirectories = 1;
            TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

            int workerCount = maxWorkerCount == 0 ? Environment.ProcessorCount : (int)maxWorkerCount;
            Task[] workers = new Task[workerCount];

            await directoryChannel.Writer.WriteAsync(root, cancellationToken);

            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (string currentDirectory in directoryChannel.Reader.ReadAllAsync(cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            try
                            {
                                foreach (var file in Directory.EnumerateFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    await fileChannel.Writer.WriteAsync(file, cancellationToken);
                                }

                                foreach (var directory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    Interlocked.Increment(ref pendingDirectories);
                                    await directoryChannel.Writer.WriteAsync(directory, cancellationToken);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                            finally
                            {
                                if (Interlocked.Decrement(ref pendingDirectories) == 0)
                                    completion.TrySetResult(true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    if (cancellationToken.IsCancellationRequested)
                        completion.TrySetResult(true);

                }, cancellationToken);
            }

            _ = completion.Task.ContinueWith(_ =>
            {
                directoryChannel.Writer.TryComplete();
                fileChannel.Writer.TryComplete();
            }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            await foreach (string file in fileChannel.Reader.ReadAllAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested) break;
                yield return file;
            }
        }

        #endregion
    }
}
