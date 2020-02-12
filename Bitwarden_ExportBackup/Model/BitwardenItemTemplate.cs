using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenItemTemplate
    {
        [JsonProperty("organizationId")]
        public Guid? OrganizationId { get; set; }

        [JsonProperty("folderId")]
        public Guid? FolderId { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("favorite")]
        public bool IsFavorite { get; set; }


        [JsonProperty("login")]
        public BitwardenLogin Login { get; set; }

        [JsonProperty("fields")]
        public BitwardenField[] Fields { get; set; }
    }
}
