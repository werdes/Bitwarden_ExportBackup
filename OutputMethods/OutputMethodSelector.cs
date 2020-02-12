using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Bitwarden_ExportBackup.OutputMethod
{
    public static class OutputMethodSelector
    {
        public static OutputMethod GetOutputMethod()
        {
            OutputMethod format = (OutputMethod)Enum.Parse<OutputMethod>(ConfigurationManager.AppSettings["EXPORT_FORMAT"]);
            return format;
        }

        public static IOutputMethod GetOutputMethod(string outputName)
        {
            OutputMethod format = GetOutputMethod();
            switch (format)
            {
                case OutputMethod.ZipAes256:
                    return new ZipAes256.ZipAes256Exporter(outputName);
                case OutputMethod.Kdbx:
                    break;
            }
            return null;
        }
    }
}
