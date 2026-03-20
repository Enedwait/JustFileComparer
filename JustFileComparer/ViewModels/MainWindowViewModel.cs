using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustFileComparerCore.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using JustFileComparerCore.FileComparers;
using JustFileComparerCore.Loggers;
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

            ElapsedTime = "---";
        }

        #endregion

        #region Compare Files

        private async Task CompareFiles()
        {
            IsProcessing = true;
            UpdateStatus($"---");

            using (_compareFilesCancellationTokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = _compareFilesCancellationTokenSource.Token;

                // ToDo: allow user to choose the comparison mode
                FileComparisonMode mode = FileComparisonMode.Size | FileComparisonMode.Hash;

                Progress<FileComparisonProgress> progress = new Progress<FileComparisonProgress>(UpdateProgress);

                //FileComparerWorkerBase worker = new FileComparerWorker();
                FileComparerWorkerBase worker = new SequentialFileComparerWorker();
                worker.OnComparisonStarted += OnComparisonStarted;
                worker.OnComparisonCompleted += OnComparisonCompleted;

                await progressLogger.BeginLog(SourceRoot, TargetRoot, mode);

                var result = await worker.CompareDirectoryContentAsync(SourceRoot, TargetRoot, mode, progress, 0, 0, cancellationToken);
                UpdateStatus(result.Success ? "Completed" : result.ErrorMessage);

                await progressLogger.EndLog(result);
            }

            IsProcessing = false;
            IsCanceled = false;
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

            Dispatcher.UIThread.Post(() =>
            {
                SuccessfulComparisonsCount = progress.SuccessfulComparisonsCount;
                FailedComparisonsCount = progress.FailedComparisonsCount;
                TotalComparisonsCount = progress.TotalComparisonsCount;
                Status = $"{progress.CurrentComparison}";
            });
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
            updateTimer = new Timer(900);
            updateTimer.AutoReset = true;
            updateTimer.Elapsed += (timer, args) =>
            {
                ElapsedTime = $"{progressLogger?.Elapsed:hh\\:mm\\:ss}";
                TotalFilesCount = (sender as FileComparerWorkerBase).FilesCount;
            };
            updateTimer.Start();

            UpdateStatus($"Started");
        }

        private void OnComparisonCompleted(object? sender, EventArgs e)
        {
            updateTimer.Stop();
            updateTimer = null;

            UpdateStatus($"Completed");
        }

        #endregion
    }
}
