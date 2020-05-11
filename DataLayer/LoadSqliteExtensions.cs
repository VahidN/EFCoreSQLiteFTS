using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace EFCoreSQLiteFTS.DataLayer
{
    public static class LoadSqliteExtensions
    {
        public static void AddToSystemPath(string extensionsDirectory)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new NotSupportedException("Modifying the path at runtime only works on Windows. On Linux and Mac, set LD_LIBRARY_PATH or DYLD_LIBRARY_PATH before running the app.");
            }

            var path = new HashSet<string>(Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator));
            if (path.Add(extensionsDirectory))
            {
                Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, path));
            }
        }
    }
}