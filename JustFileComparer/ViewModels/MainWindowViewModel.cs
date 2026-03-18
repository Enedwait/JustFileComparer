using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using JustFileComparerCore;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JustFileComparer.ViewModels
{
    public sealed partial class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private string _status;

        #endregion

        #region Properties

        public string SourceRoot { get; set; }
        public string TargetRoot { get; set; }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public bool IsProcessing { get; private set; }

        public AsyncRelayCommand Compare { get; }

        #endregion

        #region Init

        public MainWindowViewModel()
        {
            Compare = new AsyncRelayCommand(CompareFiles, CanCompareFiles);

            SourceRoot = @"X:\Data\Books";
            TargetRoot = @"X:\Data\Books - Copy";

            UpdateStatus(CanCompareFiles() ? "Ready!" : "---");
        }

        #endregion

        #region Compare Files

        public async Task CompareFiles(CancellationToken cancellationToken)
        {
            IsProcessing = true;
            UpdateStatus($"---");

            FileComparisonMode mode = FileComparisonMode.Size | FileComparisonMode.Hash;

            Progress<FileComparison> progress = new Progress<FileComparison>(comparison =>
            {
                UpdateStatus($"{Path.GetFileName(comparison.Source)}");
            });

            FileComparerWorker worker = new FileComparerWorker();
            worker.OnComparisonStarted += OnComparisonStarted;
            worker.OnComparisonCompleted += OnComparisonCompleted;

            var result = await worker.CompareDirectoryContentAsync(SourceRoot, TargetRoot, mode, progress, cancellationToken: cancellationToken);
            UpdateStatus($"S:{result.SuccessfulComparisonsCount} F:{result.FailedComparisonsCount}");

            IsProcessing = false;
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

        public bool CanCompareFiles() => !IsProcessing && !string.IsNullOrWhiteSpace(SourceRoot) && !string.IsNullOrWhiteSpace(TargetRoot);

        #endregion
    }
}
