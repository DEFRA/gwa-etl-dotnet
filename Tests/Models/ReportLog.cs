namespace Gwa.Etl.Tests.Models
{
    public class ReportLog
    {
        public int DevicesProcessed { get; set; }
        public int DevicesWithNoPhoneNumber { get; set; }
        public int DevicesWithNoUserEmailAddress { get; set; }
        public int DevicesWithUserEmailAddress { get; set; }
        public int IPads { get; set; }
    }
}
