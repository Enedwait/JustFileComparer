using System.Collections.Concurrent;
using JustFileComparerCore.Extensions;
using JustFileComparerCore.FileComparers;

namespace JustFileComparerCore.Loggers
{
    public class FileComparisonProgressLogger
    {
        #region Fields

        private string logDirectoryPath;
        private string logFilePath;
        private DateTime startedAt;
        private DateTime endedAt;
        private bool isLogStarted;
        private StreamWriter logWriter;
        private int waitForLogEntry;
        private ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private Task logTask;
        private CancellationTokenSource cancellationTokenSource;

        #endregion

        #region Properties

        public TimeSpan Elapsed => isLogStarted ? DateTime.UtcNow - startedAt : TimeSpan.Zero;

        #endregion

        #region Init

        public FileComparisonProgressLogger(int waitForLogEntryInMsec = 1000, string logDirectory = "Logs")
        {
            waitForLogEntry = waitForLogEntryInMsec;
            logDirectoryPath = Path.GetFullPath(logDirectory);
        }

        #endregion

        #region Methods

        public async Task BeginLog(string sourceRoot, string targetRoot, FileComparisonMode comparisonMode)
        {
            if (isLogStarted)
                throw new Exception("Can't begin logging, it's already begun!");

            isLogStarted = true;

            if (!Directory.Exists(logDirectoryPath))
                Directory.CreateDirectory(logDirectoryPath);

            startedAt = DateTime.UtcNow;
            logFilePath = Path.GetFullPath($"Logs//FileComparisonProgressLog_{startedAt.ToString("u").ConvertToValidFileName()}.log");

            logWriter = File.AppendText(logFilePath);
            logWriter.AutoFlush = true;

            await logWriter.WriteLineAsync($"Started at {startedAt:u}");
            await logWriter.WriteLineAsync($"Source root: {sourceRoot}");
            await logWriter.WriteLineAsync($"Target root: {targetRoot}");
            await logWriter.WriteLineAsync($"Comparison mode: {comparisonMode}");
            await logWriter.WriteLineAsync($"===PROGRESS===");
            await logWriter.FlushAsync();

            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;

            logTask = new Task(() => LogInBackground(token), token);
            logTask.Start();
        }

        private void LogInBackground(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    if (queue.TryDequeue(out string log))
                    {
                        try
                        {
                            logWriter.WriteLine(log);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    else
                    {
                        Thread.Sleep(waitForLogEntry);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task Log(FileComparisonProgress progress)
        {
            if (!isLogStarted) 
                return;

            if (progress.CurrentComparison.Result == FileComparisonResult.Equal) 
                return;

            LogToQueue($"{progress.CurrentComparison}");
        }

        private void LogToQueue(string log)
        {
            queue.Enqueue(log);
        }

        public async Task EndLog(FileComparerWorkerResult result = null)
        {
            if (!isLogStarted)
                return;

            cancellationTokenSource.Cancel();

            endedAt = DateTime.UtcNow;
            var task = Task.Run(async () =>
            {
                Thread.Sleep((int)(waitForLogEntry * 1.1f));

                try
                {
                    await logWriter.WriteLineAsync($"Completed at: {endedAt:u}");
                    await logWriter.WriteLineAsync($"Comparison took: {Elapsed:hh\\:mm\\:ss\\.fff}");

                    if (result == null)
                    {
                        await logWriter.WriteLineAsync($"Aborted");
                    }
                    else
                    {
                        if (result.Success)
                        {
                            await logWriter.WriteLineAsync($"===SUCCESS===");
                            await logWriter.WriteLineAsync($"Total Files: {result.FilesCount}");
                            await logWriter.WriteLineAsync($"Total Comparisons: {result.SuccessfulComparisonsCount + result.FailedComparisonsCount}");
                            await logWriter.WriteLineAsync($"Successful Comparisons: {result.SuccessfulComparisonsCount}");
                            await logWriter.WriteLineAsync($"Failed Comparisons: {result.FailedComparisonsCount}");
                        }
                        else
                        {
                            await logWriter.WriteLineAsync($"===FAILURE===");
                            await logWriter.WriteLineAsync($"Error: {result.ErrorMessage}");
                            await logWriter.WriteLineAsync($"Total Files: {result.FilesCount}");
                            await logWriter.WriteLineAsync($"Total Comparisons: {result.SuccessfulComparisonsCount + result.FailedComparisonsCount}");
                            await logWriter.WriteLineAsync($"Successful Comparisons: {result.SuccessfulComparisonsCount}");
                            await logWriter.WriteLineAsync($"Failed Comparisons: {result.FailedComparisonsCount}");
                        }
                    }

                    await logWriter.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    logWriter.Close();
                }
            });

            task.Wait();

            isLogStarted = false;

        }

        #endregion
    }
}
