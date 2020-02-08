using Bitwarden_ExportBackup.OutputMethod.ZipAes256.Exporter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.OutputMethod.ZipAes256
{
    public static class ZipAes256ExportSelector
    {


        public static IZipAes256Exporter GetExporter(ZipAes256ExportFormat exportFormat)
        {
            switch (exportFormat)
            {
                case ZipAes256ExportFormat.BitwardenCsv:
                    return new BitwardenCsvExporter();
                case ZipAes256ExportFormat.OnePasswordWinCsv:
                    return new OnePasswordWinCsvExporter();
                default:
                    return null;
            }
        }
    }
}
