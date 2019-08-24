using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenCard
    {
        public BitwardenCard()
        {

        }

        [JsonProperty("cardholderName")]
        public string CardholderName { get; set; }

        [JsonProperty("brand")]
        public string Brand { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("expMonth")]
        public string ExpiryMonth { get; set; }

        [JsonProperty("expYear")]
        public string ExpiryYear { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
