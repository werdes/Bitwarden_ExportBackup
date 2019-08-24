using Bitwarden_ExportBackup.ExportFormats.Exporter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.ExportFormats
{
    public static class ExportSelector
    {


        public static IExporter GetExporter(ExportFormat exportFormat)
        {
            switch (exportFormat)
            {
                case ExportFormat.BitwardenCsv:
                    return new BitwardenCsvExporter();
                case ExportFormat.OnePasswordWinCsv:
                    return new OnePasswordWinCsvExporter();
                default:
                    return null;
            }
        }
    }
}
