using JustFileComparerCore.FileEnumerations;

namespace JustFileComparerCore.Tests
{
    public sealed class FileEnumeratorTests
    {
        [Fact]
        public async void EnumerateFiles_CheckFilesCountInDifferentFolders()
        {
            var result = FileEnumerator.EnumerateFiles(@"X:\1СBase");
            Assert.Equal(82, result.Count());

            result = FileEnumerator.EnumerateFiles(@"X:\1СData");
            Assert.Equal(1, result.Count());

            result = FileEnumerator.EnumerateFiles(@"X:\HDRI");
            Assert.Equal(5887, result.Count());

            result = FileEnumerator.EnumerateFiles(@"X:\Creality");
            Assert.Equal(24, result.Count());
        }

        [Fact]
        public async void EnumerateFilesAsync_CheckFilesCountInDifferentFolders()
        {
            var result = FileEnumerator.EnumerateFiles(@"X:\1СBase");
            Assert.Equal(82, result.Count());

            result = FileEnumerator.EnumerateFiles(@"X:\1СData");
            Assert.Equal(1, result.Count());

            result = FileEnumerator.EnumerateFiles(@"X:\HDRI");
            Assert.Equal(5887, result.Count());

            result = FileEnumerator.EnumerateFiles(@"X:\Creality");
            Assert.Equal(24, result.Count());
        }
    }
}
