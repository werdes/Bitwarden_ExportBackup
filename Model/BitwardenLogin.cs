using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenLogin
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("totp")]
        public string Totp { get; set; }

        [JsonProperty("passwordRevisionDate")]
        public DateTime? RevisionDate { get; set; }

        [JsonProperty("uris")]
        public BitwardenUri[] Uris { get; set; }
    }
}
