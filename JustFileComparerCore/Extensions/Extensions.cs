namespace JustFileComparerCore.Extensions
{
    /// <summary>
    /// The <see cref="Extensions"/> class.
    /// This class contains different extension methods for the ease of work.
    /// </summary>
    public static class Extensions
    {
        #region IsSubPathOf

        /// <summary>
        /// Determines whether the current path is a sub path of the specified base path.
        /// </summary>
        /// <param name="subPath">path to be checked for nesting.</param>
        /// <param name="basePath">base path to be used as a root.</param>
        /// <param name="comparison">string comparison mode.</param>
        /// <returns><value>True</value> if the current path is actually a sub path of base path; otherwise <value>False</value>.</returns>
        public static bool IsSubPathOf(this string subPath, string basePath, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            string fullSubPath = Path.GetFullPath(subPath);
            string fullBasePath = Path.GetFullPath(basePath);

            char sep = Path.DirectorySeparatorChar;
            if (!fullBasePath.EndsWith(sep)) fullBasePath += sep;

            return fullSubPath.StartsWith(fullBasePath, comparison);
        }

        #endregion

        #region ConvertToValidFileName

        /// <summary>
        /// Converts the given input string into a valid file name.
        /// </summary>
        /// <param name="input">input string to be converted.</param>
        /// <returns>a valid file name.</returns>
        public static string ConvertToValidFileName(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                input = input.Replace(invalidChar, '-');

            return input;
        }

        #endregion

        /// <summary> Gets the current timestamp UTC. </summary>
        public static string TimeStampUtc => DateTime.UtcNow.ToString("u").ConvertToValidFileName();
    }
}
