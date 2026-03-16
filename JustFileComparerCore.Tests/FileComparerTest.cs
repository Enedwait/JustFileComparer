namespace JustFileComparerCore.Tests
{
    public sealed class FileComparerTest
    {
        [Fact]
        public async void AreFilesEqualBySize_SameFile()
        {
            var result = await FileComparer.AreFilesEqualBySizeAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async void AreFilesEqualBySize_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualBySizeAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async void AreFilesEqualByHash_SameFile()
        {
            var result = await FileComparer.AreFilesEqualByHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async void AreFilesEqualByHash_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualByHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async void AreFilesEqualByBytes_SameFile()
        {
            var result = await FileComparer.AreFilesEqualByteByByteAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async void AreFilesEqualByBytes_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualByteByByteAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async void AreFilesEqualBySizeAndHash_SameFile()
        {
            var result = await FileComparer.AreFilesEqualBySizeAndHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async void AreFilesEqualBySizeAndHash_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualBySizeAndHashAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }

        [Fact]
        public async void AreFilesEqual_SameFile()
        {
            var result = await FileComparer.AreFilesEqualAsync(@"TestFiles/f1.jpg", @"TestFiles/f1.jpg");

            Assert.Equal(true, result);
        }

        [Fact]
        public async void AreFilesEqual_DifferentFiles()
        {
            var result = await FileComparer.AreFilesEqualAsync(@"TestFiles/f1.jpg", @"TestFiles/f2.bmp");

            Assert.Equal(false, result);
        }
    }
}
