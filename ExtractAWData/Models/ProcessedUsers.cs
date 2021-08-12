using System.Collections.Generic;

namespace Gwa.Etl.Models
{
    public class ProcessedUsers
    {
        public int DeviceCount { get; set; }
        public int IPadCount { get; set; }
        public int NoEmailCount { get; set; }
        public int NoPhoneNumberCount { get; set; }
        public IDictionary<string, User> Users { get; set; }
    }
}
