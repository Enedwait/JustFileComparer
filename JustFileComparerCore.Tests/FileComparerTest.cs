using JustFileComparerCore.FileComparers;

namespace JustFileComparerCore.Tests
{
    /// <summary>
    /// <remarks>These tests are author's system specific and need to be adapted if used on any other system!</remarks>
    /// </summary>
    public sealed class FileComparerTest
    {
        [Fact]
        public async Task AreFilesEqualBySize_SameFile()
        {
            var result = await FileComparer.AreFilesEqualBySizeAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async Task AreFilesEqualBySize_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualBySizeAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async Task AreFilesEqualByHash_SameFile()
        {
            var result = await FileComparer.AreFilesEqualByHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async Task AreFilesEqualByHash_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualByHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async Task AreFilesEqualByBytes_SameFile()
        {
            var result = await FileComparer.AreFilesEqualByteByByteAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async Task AreFilesEqualByBytes_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualByteByByteAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async Task AreFilesEqualBySizeAndHash_SameFile()
        {
            var result = await FileComparer.AreFilesEqualBySizeAndHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async Task AreFilesEqualBySizeAndHash_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualBySizeAndHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async Task AreFilesEqual_SameFile()
        {
            var result = await FileComparer.AreFilesEqualAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async Task AreFilesEqual_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }
    }
}
