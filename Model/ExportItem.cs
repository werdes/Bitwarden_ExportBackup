using Bitwarden_ExportBackup.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class ExportItem
    {
        public string Folder { get; set; }
        public bool Favorite { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public string Fields { get; set; }
        public string LoginUris { get; set; }
        public string LoginUsername { get; set; }
        public string LoginPassword { get; set; }
        public string LoginTotp { get; set; }

        public string ToCsv()
        {
            return new string[]
            {
                Folder,
                (Favorite ? "1" : "0"),
                Type,
                Name,
                Notes,
                Fields,
                LoginUris,
                LoginUsername,
                LoginPassword,
                LoginTotp
            }.Join(";");
        }
    }
}
