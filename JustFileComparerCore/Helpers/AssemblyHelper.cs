using System.Reflection;

namespace JustFileComparerCore.Helpers
{
    /// <summary>
    /// The <see cref="AssemblyHelper"/> class.
    /// This class allows to retrieve application/assembly specific info.
    /// </summary>
    public static class AssemblyHelper
    {
        /// <summary> Gets a string containing the product name. </summary>
        public static string Product => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyProductAttribute>()?.Product;

        /// <summary> Get a string containing the version info. </summary>
        public static string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        /// <summary> Gets a string containing the company name. </summary>
        public static string Company => Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
    }
}
