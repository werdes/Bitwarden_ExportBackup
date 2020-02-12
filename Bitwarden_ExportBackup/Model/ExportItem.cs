using Bitwarden_ExportBackup.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class ExportItem
    {
        public ExportItem()
        {
            Collections = new List<BitwardenObject>();
        }

        public BitwardenObject Folder { get; set; }
        public BitwardenObject Object { get; set; }
        public List<BitwardenObject> Collections { get; set; }
        public BitwardenObject Organization { get; set; }

    }
}
