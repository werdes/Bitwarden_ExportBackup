using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenField
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }
    }
}
