using System.Collections.Generic;

namespace Gwa.Etl.Models
{
    public class AirWatchApiResponse
    {
        public IList<Device> Devices { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}
