using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Bitwarden_ExportBackup.Extensions
{
    public static class ProcessExtensions
    {
        public static string GetLogMessage(this Process p)
        {
            return $"\"{p.StartInfo.FileName}\" {p.StartInfo.Arguments}";
        }
    }
}
