using Bitwarden_ExportBackup.OutputMethod;
using Bitwarden_ExportBackup.OutputMethod.ZipAes256;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model.Exceptions
{
    public class UnsupportedItemTypeException : Exception
    {
        public IZipAes256Exporter Exporter { get; set; }
        public BitwardenObject.ItemType ItemType { get; set; }

        public UnsupportedItemTypeException(IZipAes256Exporter exporter, BitwardenObject.ItemType itemType)
        {
            Exporter = exporter;
            ItemType = itemType;
        }
    }
}
