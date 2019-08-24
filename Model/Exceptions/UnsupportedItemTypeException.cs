using Bitwarden_ExportBackup.ExportFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model.Exceptions
{
    public class UnsupportedItemTypeException : Exception
    {
        public IExporter Exporter { get; set; }
        public BitwardenObject.ItemType ItemType { get; set; }

        public UnsupportedItemTypeException(IExporter exporter, BitwardenObject.ItemType itemType)
        {
            Exporter = exporter;
            ItemType = itemType;
        }
    }
}
