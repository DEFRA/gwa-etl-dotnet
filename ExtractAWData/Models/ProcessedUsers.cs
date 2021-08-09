using System.Collections.Generic;

namespace Defra.Gwa.Etl
{
    public class ProcessedUsers
    {
        public IDictionary<string, User> Users { get; set; }
        public int DeviceCount { get; set; }
        public int IPadCount { get; set; }
        public int NoEmailCount { get; set; }
        public int NoPhoneNumberCount { get; set; }
    }
}
