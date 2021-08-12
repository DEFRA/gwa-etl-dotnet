using Gwa.Etl.Services;
using Moq;
using System.Net.Http;

namespace Gwa.Etl.Tests.Models
{
    public class AirWatchServiceTestSetup
    {
        public AirWatchService AirWatchService { get; set; }
        public Mock<HttpMessageHandler> HandlerMock { get; set; }
    }
}
