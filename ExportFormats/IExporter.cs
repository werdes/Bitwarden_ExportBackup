using Bitwarden_ExportBackup.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.ExportFormats
{
    public interface IExporter
    {
        byte[] GetFileContent(List<ExportItem> items, BitwardenObject.ItemType itemType);

        string GetFileExtension();

        BitwardenObject.ItemType[] GetSupportedItemTypes();
        
    }
}
