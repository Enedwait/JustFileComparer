using System.Security.Cryptography;

namespace JustFileComparerCore.FileComparers
{
    public sealed class FileComparer
    {
        delegate Task<bool> FileCompareDelegate(string sourceFilePath, string targetFilePath, CancellationToken cancellationToken = default);

        #region Comparison Methods

        public static async Task<bool> AreFilesEqualAsync(string sourceFilePath, string targetFilePath, CancellationToken cancellationToken = default)
            => await AreFilesEqualAsync(sourceFilePath, targetFilePath, FileComparisonMode.Size | FileComparisonMode.Hash | FileComparisonMode.Bytes, cancellationToken);

        public static async Task<bool> AreFilesEqualBySizeAndHashAsync(string sourceFilePath, string targetFilePath, CancellationToken cancellationToken = default)
            => await AreFilesEqualAsync(sourceFilePath, targetFilePath, FileComparisonMode.Size | FileComparisonMode.Hash, cancellationToken);

        public static async Task<bool> AreFilesEqualAsync(string sourceFilePath, string targetFilePath, FileComparisonMode mode, CancellationToken cancellationToken = default)
        {
            if (mode == FileComparisonMode.None) return false;

            return await Check(sourceFilePath, targetFilePath, mode, FileComparisonMode.Size, AreFilesEqualBySizeAsync, cancellationToken) &&
                   await Check(sourceFilePath, targetFilePath, mode, FileComparisonMode.Hash, AreFilesEqualByHashAsync, cancellationToken) &&
                   await Check(sourceFilePath, targetFilePath, mode, FileComparisonMode.Bytes, AreFilesEqualByteByByteAsync, cancellationToken);
        }

        private static async Task<bool> Check(string sourceFilePath, string targetFilePath, FileComparisonMode mode, FileComparisonMode flag, FileCompareDelegate fileCompare, CancellationToken cancellationToken = default)
        {
            if (mode.HasFlag(flag))
                return await fileCompare(sourceFilePath, targetFilePath, cancellationToken);

            return true;
        }

        #endregion

        #region Single Comparison Methods

        public static async Task<long> GetFileSizeAsync(string filePath) =>
            await Task.Run(() => new FileInfo(filePath).Length);

        public static async Task<bool> AreFilesEqualBySizeAsync(string sourceFilePath, string targetFilePath, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            long sourceSize = await GetFileSizeAsync(sourceFilePath);
            long targetSize = await GetFileSizeAsync(targetFilePath);

            return sourceSize == targetSize;
        }

        public static async Task<bool> AreFilesEqualByHashAsync(string sourceFilePath, string targetFilePath, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            using (HashAlgorithm hash = SHA256.Create())
            using (FileStream source = File.OpenRead(sourceFilePath))
            using (FileStream target = File.OpenRead(targetFilePath))
            {
                if (cancellationToken.IsCancellationRequested) return false;
                byte[] sourceHash = await hash.ComputeHashAsync(source, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return false;
                byte[] targetHash = await hash.ComputeHashAsync(target, cancellationToken);

                if (cancellationToken.IsCancellationRequested) return false;
                return AreHashesEqual(sourceHash, targetHash);
            }
        }

        public static async Task<bool> AreFilesEqualByteByByteAsync(string sourceFilePath, string targetFilePath, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            using (FileStream source = File.OpenRead(sourceFilePath))
            using (FileStream target = File.OpenRead(targetFilePath))
            {
                if (cancellationToken.IsCancellationRequested) return false;

                // ToDo: implement more performant and async option
                if (source.ReadByte() != target.ReadByte())
                    return false;
            }

            return true;
        }

        #endregion

        #region Utility Methods

        public static bool AreHashesEqual(byte[] hashA, byte[] hashB)
        {
            if (hashA.Length != hashB.Length) return false;

            for (int i = 0; i < hashA.Length; i++)
                if (hashA[i] != hashB[i]) return false;

            return true;
        }

        #endregion
    }
}
