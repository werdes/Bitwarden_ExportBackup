using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenObject
    {
        public enum ItemType
        {
            Login = 1,
            SecureNote = 2,
            Card = 3,
            Identity = 4
        }

        public BitwardenObject()
        {
            CollectionIds = new List<Guid>();
        }

        [JsonProperty("object")]
        public string Object { get; set; }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("organizationId")]
        public Guid? OrganizationId { get; set; }

        [JsonProperty("folderId")]
        public Guid? FolderId { get; set; }

        [JsonProperty("type")]
        public ItemType Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("favorite")]
        public bool IsFavorite { get; set; }
        
        [JsonProperty("collectionIds")]
        public List<Guid> CollectionIds { get; set; }
        
        [JsonProperty("attachments")]
        public List<BitwardenAttachment> Attachments { get; set; }

        [JsonProperty("revisionDate")]
        public DateTime? RevisionDate { get; set; }

        [JsonProperty("login")]
        public BitwardenLogin Login { get; set; }

        [JsonProperty("card")]
        public BitwardenCard Card { get; set; }

        [JsonProperty("secureNote")]
        public BitwardenSecureNote SecureNote { get; set; }

        [JsonProperty("identity")]
        public BitwardenIdentity Identity { get; set; }

        [JsonProperty("fields")]
        public BitwardenField[] Fields { get; set; }
    }
}
