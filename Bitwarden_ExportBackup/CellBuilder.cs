using Bitwarden_ExportBackup.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bitwarden_ExportBackup
{
    public static class CellBuilder
    {
        public static readonly char[] INVALID_CHARS = { '\r', '\n' };

        public static string[] BuildCells(params object[] cells)
        {
            string[] retVal = new string[cells.Length].Select(x => string.Empty).ToArray();
            for (int i = 0; i < cells.Length; i++)
            {
                object obj = cells[i];
                if (obj != null)
                {
                    string strObj = obj.ToString();
                    if (strObj.ContainsAny(INVALID_CHARS) ||
                        strObj.Trim() != strObj)
                    {
                        strObj = strObj.EnvelopWith("\"");
                    }

                    retVal[i] = strObj;
                }
            }

            return retVal;
        }
    }
}
