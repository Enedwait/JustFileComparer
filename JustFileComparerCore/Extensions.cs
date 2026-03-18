namespace JustFileComparerCore
{
    public static class Extensions
    {
        public static bool IsInside(this string subPath, string basePath, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            string fullSubPath = Path.GetFullPath(subPath);
            string fullBasePath = Path.GetFullPath(basePath);

            char sep = Path.DirectorySeparatorChar;
            if (!fullBasePath.EndsWith(sep)) fullBasePath += sep;

            return fullSubPath.StartsWith(fullBasePath, comparison);
        }
    }
}
