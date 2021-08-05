using System.Collections.Generic;
using Newtonsoft.Json;

namespace Defra.Gwa.Etl
{
    public class User
    {
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }
        [JsonProperty("phoneNumbers")]
        public IList<string> PhoneNumbers { get; set; }
    }
}
