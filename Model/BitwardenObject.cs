using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenObject
    {
        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

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
        
        [JsonProperty("collectionIds")]
        public Guid[] CollectionIds { get; set; }
        
        [JsonProperty("revisionDate")]
        public DateTime? RevisionDate { get; set; }

        [JsonProperty("login")]
        public BitwardenLogin Login { get; set; }

        [JsonProperty("fields")]
        public BitwardenField[] Fields { get; set; }
    }
}
