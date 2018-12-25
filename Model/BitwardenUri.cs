using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenUri
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }
}
