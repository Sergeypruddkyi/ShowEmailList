using System;
using System.IO;

namespace ShowEmailList
{
    public static class AppPaths
    {
        public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string SettingsPath = Path.Combine(BaseDir, "settings.ini");
        public static readonly string AccountsPath = Path.Combine(BaseDir, "accounts.json");
    }
}
