using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JustFileComparerCore;
using JustFileComparerCore.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using JustFileComparerCore.FileComparers;

namespace JustFileComparer.ViewModels
{
    public sealed partial class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private CancellationTokenSource _compareFilesCancellationTokenSource;
        private bool _isProcessing;
        [ObservableProperty] private string _status;
        [ObservableProperty] private ulong _totalComparisonsCount;
        [ObservableProperty] private ulong _successfulComparisonsCount;
        [ObservableProperty] private ulong _failedComparisonsCount;
        private string _sourceRoot;
        private string _targetRoot;

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

#if DEBUG
            SourceRoot = @"X:\Data\Books";
            TargetRoot = @"X:\Data\Books - Copy";

            UpdateStatus(CanCompareFiles() ? "Ready!" : "---");
#endif
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

                FileComparisonMode mode = FileComparisonMode.Size | FileComparisonMode.Hash;

                Progress<FileComparisonProgress> progress = new Progress<FileComparisonProgress>(UpdateProgress);

                FileComparerWorker worker = new FileComparerWorker();
                worker.OnComparisonStarted += OnComparisonStarted;
                worker.OnComparisonCompleted += OnComparisonCompleted;

                var result = await worker.CompareDirectoryContentAsync(SourceRoot, TargetRoot, mode, progress, cancellationToken: cancellationToken);
                UpdateStatus(result.ToString());
            }

            IsProcessing = false;
        }

        public bool CanCompareFiles() => !IsProcessing && !string.IsNullOrWhiteSpace(SourceRoot) && !string.IsNullOrWhiteSpace(TargetRoot);

        #endregion

        #region Cancel Compare Files

        private async Task CancelCompareFiles()
        {
            if (!IsProcessing || _compareFilesCancellationTokenSource == null) return;

            await _compareFilesCancellationTokenSource.CancelAsync();
        }

        public bool CanCancelCompareFiles() => IsProcessing;

        #endregion

        #region Methods

        private void UpdateProgress(FileComparisonProgress progress)
        {
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

        private void OnComparisonStarted(object? sender, EventArgs e)
        {
            UpdateStatus($"Started");
        }

        private void OnComparisonCompleted(object? sender, EventArgs e)
        {
            UpdateStatus($"Completed");
        }

        #endregion
    }
}
