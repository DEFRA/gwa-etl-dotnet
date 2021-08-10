using System;

namespace Gwa.Etl.Models
{
    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime Next { get; set; }
    }
}
