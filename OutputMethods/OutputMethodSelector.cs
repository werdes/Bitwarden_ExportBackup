using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace Bitwarden_ExportBackup.OutputMethod
{
    public static class OutputMethodSelector
    {
        /// <summary>
        /// Returns the configured ouput method
        /// </summary>
        /// <returns></returns>
        public static OutputMethod GetOutputMethod()
        {
            OutputMethod format = (OutputMethod)Enum.Parse<OutputMethod>(ConfigurationManager.AppSettings["OUTPUT_METHOD"]);
            return format;
        }

        /// <summary>
        /// Returns an implementation representing the configured ouput method
        /// </summary>
        /// <param name="outputName"></param>
        /// <returns></returns>
        public static IOutputMethod GetOutputMethod(string outputName)
        {
            OutputMethod format = GetOutputMethod();
            switch (format)
            {
                case OutputMethod.ZipAes256:
                    return new ZipAes256.ZipAes256Exporter(outputName);
                case OutputMethod.Kdbx:
                    return null;
            }
            return null;
        }
    }
}
