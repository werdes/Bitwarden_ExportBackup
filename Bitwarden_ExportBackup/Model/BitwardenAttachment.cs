using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenAttachment
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("sizeName")]
        public string SizeName { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
