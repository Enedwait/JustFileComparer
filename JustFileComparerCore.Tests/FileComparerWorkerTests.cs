using JustFileComparerCore.FileComparers;

namespace JustFileComparerCore.Tests
{
    /// <summary>
    /// <remarks>These tests are author's system specific and need to be adapted if used on any other system!</remarks>
    /// </summary>
    public sealed class FileComparerWorkerTests
    {
        private static FileComparisonMode Mode = FileComparisonMode.Size | FileComparisonMode.Hash;
        private Progress<FileComparisonProgress> Progress = new Progress<FileComparisonProgress>();

        [Fact]
        public async Task CompareContentAsync_SmallFolder()
        {
            FileComparerWorker worker = new FileComparerWorker();

            var result = await worker.CompareDirectoryContentAsync(@"X:\Data\Pictures", @"X:\Data\Pictures - Copy", Mode, Progress);
            Assert.Equal(2, result.FailedComparisons.Count);
            Assert.Equal((ulong)5, result.SuccessfulComparisonsCount);
        }

        [Fact]
        public async Task CompareContentAsync_MediumFolder()
        {
            FileComparerWorker worker = new FileComparerWorker();
            
            var result = await worker.CompareDirectoryContentAsync(@"X:\Data\Books", @"X:\Data\Books - Copy", Mode, Progress);
            Assert.Equal(0, result.FailedComparisons.Count);
            Assert.Equal((ulong)2663, result.SuccessfulComparisonsCount);
        }
    }
}
