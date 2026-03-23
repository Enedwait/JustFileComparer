using System.Collections.Concurrent;
using JustFileComparerCore.Extensions;
using JustFileComparerCore.FileComparers;

namespace JustFileComparerCore.Loggers
{
    public class FileComparisonProgressLogger
    {
        #region Fields

        private string logDirectoryPath = string.Empty;
        private string logFilePath = string.Empty;
        private DateTime startedAt;
        private DateTime endedAt;
        private State state;
        private StreamWriter logWriter = null!;
        private int waitForLogEntry;
        private ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private Task logTask = null!;
        private CancellationTokenSource cancellationTokenSource = null!;

        #endregion

        #region Properties

        public TimeSpan Elapsed => 
            state == State.Started ? GetCurrentElapsedTime() : 
            state is State.Ended or State.Completed ? endedAt - startedAt : 
            TimeSpan.Zero;

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
            if (state == State.Started)
                throw new Exception("Can't begin logging, it's already begun!");

            if (state == State.Ended)
                throw new Exception("Can't begin logging, it's not completed yet!");

            state = State.Started;

            if (!Directory.Exists(logDirectoryPath))
                Directory.CreateDirectory(logDirectoryPath);

            startedAt = DateTime.UtcNow;
            logFilePath = Path.GetFullPath(Path.Combine(logDirectoryPath, $"FileComparisonProgressLog_{startedAt.ToString("u").ConvertToValidFileName()}.log"));

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
                    if (queue.TryDequeue(out string? log))
                    {
                        try
                        {
                            logWriter.WriteLine(log);
                        }
                        catch (Exception ex)
                        {
#if DEBUG
                            Console.WriteLine(ex);
#endif
                        }
                    }
                    else
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        Thread.Sleep(waitForLogEntry);
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine(ex);
#endif
            }
        }

        public async Task Log(FileComparisonProgress progress)
        {
            if (state is not State.Started) 
                return;

            if (progress.CurrentComparison.Result == FileComparisonResult.Equal) 
                return;

            await Task.Run(() => LogToQueue($"{progress.CurrentComparison}"));
        }

        private void LogToQueue(string log)
        {
            queue.Enqueue(log);
        }

        public async Task EndLog(FileComparerWorkerResult? result = null)
        {
            if (state != State.Started)
                return;

            endedAt = DateTime.UtcNow;
            state = State.Ended;

            cancellationTokenSource.Cancel();
            
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
#if DEBUG
                    Console.WriteLine(ex);
#endif
                }
                finally
                {
                    logWriter.Close();
                }

                state = State.Completed;
            });

            await task.WaitAsync(CancellationToken.None);
        }

        private TimeSpan GetCurrentElapsedTime() => DateTime.UtcNow - startedAt;

        private enum State { None, Started, Ended, Completed }

        #endregion
    }
}
