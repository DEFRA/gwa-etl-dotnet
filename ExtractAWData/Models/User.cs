using System.Collections.Generic;
using Newtonsoft.Json;

namespace Gwa.Etl.Models
{
    public class User
    {
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("phoneNumbers")]
        public IList<string> PhoneNumbers { get; set; }
    }
}
