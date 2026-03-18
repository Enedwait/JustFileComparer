using System.Security.Cryptography;

namespace JustFileComparerCore
{
    public sealed class FileComparer
    {
        delegate Task<bool> FileCompareDelegate(string sourceFilePath, string targetFilePath);

        #region Comparison Methods

        public static async Task<bool> AreFilesEqualAsync(string sourceFilePath, string targetFilePath)
            => await AreFilesEqualAsync(sourceFilePath, targetFilePath, FileComparisonMode.Size | FileComparisonMode.Hash | FileComparisonMode.Bytes);

        public static async Task<bool> AreFilesEqualBySizeAndHashAsync(string sourceFilePath, string targetFilePath)
            => await AreFilesEqualAsync(sourceFilePath, targetFilePath, FileComparisonMode.Size | FileComparisonMode.Hash);

        public static async Task<bool> AreFilesEqualAsync(string sourceFilePath, string targetFilePath, FileComparisonMode mode)
        {
            if (mode == FileComparisonMode.None) return false;

            return await Check(sourceFilePath, targetFilePath, mode, FileComparisonMode.Size, AreFilesEqualBySizeAsync) &&
                   await Check(sourceFilePath, targetFilePath, mode, FileComparisonMode.Hash, AreFilesEqualByHashAsync) &&
                   await Check(sourceFilePath, targetFilePath, mode, FileComparisonMode.Bytes, AreFilesEqualByteByByteAsync);
        }

        private static async Task<bool> Check(string sourceFilePath, string targetFilePath, FileComparisonMode mode, FileComparisonMode flag, FileCompareDelegate fileCompare)
        {
            if (mode.HasFlag(flag))
                return await fileCompare(sourceFilePath, targetFilePath);

            return true;
        }

        #endregion

        #region Single Comparison Methods

        public static async Task<bool> AreFilesEqualBySizeAsync(string sourceFilePath, string targetFilePath)
        {
            FileInfo source = new FileInfo(sourceFilePath);
            FileInfo target = new FileInfo(targetFilePath);

            return source.Length == target.Length;
        }

        public static async Task<bool> AreFilesEqualByHashAsync(string sourceFilePath, string targetFilePath)
        {
            using (HashAlgorithm hash = SHA256.Create())
            using (FileStream source = File.OpenRead(sourceFilePath))
            using (FileStream target = File.OpenRead(targetFilePath))
            {
                byte[] sourceHash = await hash.ComputeHashAsync(source);
                byte[] targetHash = await hash.ComputeHashAsync(target);

                return AreHashesEqual(sourceHash, targetHash);
            }
        }

        public static async Task<bool> AreFilesEqualByteByByteAsync(string sourceFilePath, string targetFilePath)
        {
            using (FileStream source = File.OpenRead(sourceFilePath))
            using (FileStream target = File.OpenRead(targetFilePath))
            {
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
