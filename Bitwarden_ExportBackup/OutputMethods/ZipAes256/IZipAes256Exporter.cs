using Bitwarden_ExportBackup.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.OutputMethod.ZipAes256
{
    public interface IZipAes256Exporter
    {
        byte[] GetFileContent(List<ExportItem> items, BitwardenObject.ItemType itemType);

        string GetFileExtension();

        BitwardenObject.ItemType[] GetSupportedItemTypes();
        
    }
}
