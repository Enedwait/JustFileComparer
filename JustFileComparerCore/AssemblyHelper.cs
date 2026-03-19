using System.Reflection;

namespace JustFileComparerCore
{
    public static class AssemblyHelper
    {
        public static string Product => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

        public static string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        public static string Company => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
    }
}
