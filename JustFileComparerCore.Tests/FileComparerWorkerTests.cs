namespace JustFileComparerCore.Tests
{
    public sealed class FileComparerWorkerTests
    {
        [Fact]
        public async void CompareContentAsync_SmallFolder()
        {
            FileComparerWorker worker = new FileComparerWorker();

            var result = await worker.CompareDirectoryContentAsync(@"X:\Data\Pictures", @"X:\Data\Pictures - Copy");
            Assert.Equal(2, result.FailedComparisons.Count);
            Assert.Equal((ulong)5, result.SuccessfulComparisonsCount);
        }

        [Fact]
        public async void CompareContentAsync_MediumFolder()
        {
            FileComparerWorker worker = new FileComparerWorker();
            
            var result = await worker.CompareDirectoryContentAsync(@"X:\Data\Books", @"X:\Data\Books - Copy");
            Assert.Equal(0, result.FailedComparisons.Count);
            Assert.Equal((ulong)2663, result.SuccessfulComparisonsCount);
        }
    }
}
