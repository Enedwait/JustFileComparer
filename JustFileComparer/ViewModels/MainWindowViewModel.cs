using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustFileComparerCore.FileComparers;
using JustFileComparerCore.Helpers;
using JustFileComparerCore.Loggers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace JustFileComparer.ViewModels
{
    public sealed partial class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private Timer updateTimer;
        private CancellationTokenSource _compareFilesCancellationTokenSource;
        private bool _isProcessing;
        private bool _isCanceled;
        [ObservableProperty] private string _status;
        [ObservableProperty] private string _elapsedTime;
        [ObservableProperty] private ulong _totalFilesCount;
        [ObservableProperty] private ulong _totalComparisonsCount;
        [ObservableProperty] private ulong _successfulComparisonsCount;
        [ObservableProperty] private ulong _failedComparisonsCount;
        private string _sourceRoot;
        private string _targetRoot;
        private FileComparisonProgressLogger progressLogger;
        private FileComparerWorkerBase worker;
        private FileComparisonProgress lastProgress;

        #endregion

        #region Properties

        public string AppInfo => $"{AssemblyHelper.Product} [v{AssemblyHelper.Version}] by {AssemblyHelper.Company}";

        public string SourceRoot
        {
            get => _sourceRoot;
            set
            {
                if (SetProperty(ref _sourceRoot, value))
                {
                    Compare?.NotifyCanExecuteChanged();
                    UpdateStatusIfCanCompare();
                }
            }
        }

        public string TargetRoot
        {
            get => _targetRoot;
            set
            {
                if (SetProperty(ref _targetRoot, value))
                {
                    Compare?.NotifyCanExecuteChanged();
                    UpdateStatusIfCanCompare();
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            private set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    Compare?.NotifyCanExecuteChanged();
                    CancelCompare?.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsCanceled
        {
            get => _isCanceled;
            private set
            {
                if (SetProperty(ref _isCanceled, value))
                {
                    Compare?.NotifyCanExecuteChanged();
                    CancelCompare?.NotifyCanExecuteChanged();
                }
            }
        }

        public bool DoCompareBySize { get; set; }
        public bool DoCompareByHash { get; set; }
        public bool DoCompareByBytes { get; set; }

        #endregion

        #region Commands

        public AsyncRelayCommand Compare { get; }
        public AsyncRelayCommand CancelCompare { get; }

        #endregion

        #region Init

        public MainWindowViewModel()
        {
            Compare = new AsyncRelayCommand(CompareFiles, CanCompareFiles);
            CancelCompare = new AsyncRelayCommand(CancelCompareFiles, CanCancelCompareFiles);

            progressLogger = new FileComparisonProgressLogger();

            DoCompareBySize = true;
            DoCompareByHash = true;

            ResetInfo();
#if DEBUG
            DoCompareByHash = false;
            SourceRoot = @"U:\";
            TargetRoot = @"W:\";
#endif
        }

        #endregion

        #region Compare Files

        private async Task CompareFiles()
        {
            var task = Task.Run(async () =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    lastProgress = new FileComparisonProgress();
                    IsProcessing = true;
                    ResetInfo();
                });

                using (_compareFilesCancellationTokenSource = new CancellationTokenSource())
                {
                    CancellationToken cancellationToken = _compareFilesCancellationTokenSource.Token;

                    FileComparisonMode mode = FileComparisonMode.None;
                    if (DoCompareBySize) mode |= FileComparisonMode.Size;
                    if (DoCompareByHash) mode |= FileComparisonMode.Hash;
                    if (DoCompareByBytes) mode |= FileComparisonMode.Bytes;

                    Progress<FileComparisonProgress> progress = new Progress<FileComparisonProgress>(UpdateProgress);

                    worker = new FileComparerWorker();
                    //worker = new SequentialFileComparerWorker();
                    worker.OnComparisonStarted += OnComparisonStarted;
                    worker.OnComparisonCompleted += OnComparisonCompleted;

                    await progressLogger.BeginLog(SourceRoot, TargetRoot, mode);

                    var result = await worker.CompareDirectoryContentAsync(SourceRoot, TargetRoot, mode, progress, 0, 0, cancellationToken);
                    UpdateStatus(result.Success ? "Completed" : result.ErrorMessage);

                    await progressLogger.EndLog(result);
                }

                Dispatcher.UIThread.Invoke(() =>
                {
                    IsProcessing = false;
                    IsCanceled = false;
                    UpdateInfo();
                });
            });
        }

        public bool CanCompareFiles() => !IsProcessing && !IsCanceled && !string.IsNullOrWhiteSpace(SourceRoot) && !string.IsNullOrWhiteSpace(TargetRoot);

        #endregion

        #region Cancel Compare Files

        private async Task CancelCompareFiles()
        {
            if (!IsProcessing || IsCanceled) return;

            IsCanceled = true;

            await _compareFilesCancellationTokenSource.CancelAsync();
        }

        public bool CanCancelCompareFiles() => IsProcessing && !IsCanceled;

        #endregion

        #region Methods

        private async void UpdateProgress(FileComparisonProgress progress)
        {
            await progressLogger.Log(progress);
            lastProgress = progress;
        }

        private void UpdateStatus(string message)
        {
            Dispatcher.UIThread.Post(() => Status = message);
        }

        private void UpdateStatusIfCanCompare()
        {
            if (CanCompareFiles()) UpdateStatus("Ready to compare!");
            else if ("Ready to compare!".Equals(_status)) UpdateStatus("");
        }

        private void OnComparisonStarted(object? sender, EventArgs e)
        {
            updateTimer = new Timer(100);
            updateTimer.AutoReset = true;
            updateTimer.Elapsed += (timer, args) => UpdateInfo();
            updateTimer.Start();

            Dispatcher.UIThread.Invoke(() =>
            {
                UpdateStatus($"Started");
            });
        }

        private void OnComparisonCompleted(object? sender, EventArgs e)
        {
            updateTimer.Stop();
            updateTimer = null;

            Dispatcher.UIThread.Invoke(() =>
            {
                UpdateStatus($"Completed");
                UpdateInfo();
            });
        }

        private void UpdateInfo()
        {
            ElapsedTime = $"{progressLogger?.Elapsed:hh\\:mm\\:ss}";
            TotalFilesCount = worker.FilesCount;
            SuccessfulComparisonsCount = lastProgress.SuccessfulComparisonsCount;
            FailedComparisonsCount = lastProgress.FailedComparisonsCount;
            TotalComparisonsCount = lastProgress.TotalComparisonsCount;
            if (lastProgress.CurrentComparison.Result != FileComparisonResult.None) 
                Status = $"{lastProgress.CurrentComparison}";
        }

        private void ResetInfo()
        {
            ElapsedTime ="---";
            TotalFilesCount = 0;
            SuccessfulComparisonsCount = 0;
            FailedComparisonsCount = 0;
            TotalComparisonsCount = 0;
            Status = $"";
        }

        #endregion
    }
}
