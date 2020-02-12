using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bitwarden_ExportBackup.Model
{
    public class BitwardenSecureNote
    {
        public enum SecureNoteType
        {
            Generic = 0
        }

        public BitwardenSecureNote()
        {

        }

        [JsonProperty("type")]
        public SecureNoteType Type { get; set; }
    }
}
