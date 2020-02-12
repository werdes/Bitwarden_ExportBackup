using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Extensions
{
    public static class StringExtensions
    {
        public static string ToBase64(this string s, Encoding encoding)
        {
            byte[] bytes = encoding.GetBytes(s);
            return Convert.ToBase64String(bytes);
        }

        public static string EnvelopWith(this string s, string env)
        {
            return env + s + env;
        }

    }
}
