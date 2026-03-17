using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace JustFileComparerCore
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
        /// <returns>The <see cref="IEnumerable{String}"/> containing the full paths of all files found as requested.</returns>
        public static IEnumerable<string> EnumerateFiles(string root, string searchPattern = "*", uint maxWorkerCount = 0)
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
                                break;

                            try
                            {
                                foreach (var file in Directory.EnumerateFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly))
                                    files.Add(file);

                                foreach (var directory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                                {
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
                                countdown.Signal();
                            }
                        }
                    });
                }

                countdown.Wait();

                Task.WaitAll(workers);
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
        public static async IAsyncEnumerable<string> EnumerateFilesAsync(string root, string searchPattern = "*", uint maxWorkerCount = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                            try
                            {
                                foreach (var file in Directory.EnumerateFiles(currentDirectory, searchPattern, SearchOption.TopDirectoryOnly))
                                    await fileChannel.Writer.WriteAsync(file, cancellationToken);

                                foreach (var directory in Directory.EnumerateDirectories(currentDirectory, "*", SearchOption.TopDirectoryOnly))
                                {
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
                }, cancellationToken);
            }

            _ = completion.Task.ContinueWith(_ =>
            {
                directoryChannel.Writer.TryComplete();
                fileChannel.Writer.TryComplete();
            }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            await foreach (string file in fileChannel.Reader.ReadAllAsync(cancellationToken))
                yield return file;
        }

        #endregion
    }
}
