using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// exe'nin "Özellikler > Ayrıntılar" bilgileri
[assembly: AssemblyTitle("Broken Hub")]
[assembly: AssemblyDescription("Installs, updates and opens the Broken tools")]
[assembly: AssemblyCompany("HErkanuzun")]
[assembly: AssemblyProduct("Broken Hub")]
[assembly: AssemblyCopyright("Copyright (c) 2026 HErkanuzun. MIT License.")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: ComVisible(false)]

namespace BrokenHub
{
    static class AppInfo
    {
        public const string Name = "Broken Hub";
        public const string Version = "1.1";
        public const string Repo = "https://github.com/HErkanuzun/BrokenHub";
    }
}
