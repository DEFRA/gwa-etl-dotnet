using Azure.Storage.Blobs;
using Gwa.Etl.Models;
using Gwa.Etl.Tests.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gwa.Etl.Tests.Helpers
{
    public class AirWatchServiceTests
    {
        private readonly IConfiguration configuration;
        private readonly Mock<BlobClient> blobClientMock = new();
        private readonly Mock<IHttpClientFactory> httpClientFactoryMock = new();
        private readonly Mock<ILogger<ExtractAWData>> loggerMock = new();

        public AirWatchServiceTests()
        {
            IDictionary<string, string> config = new Dictionary<string, string>()
            {
                { "AW_DOMAIN", "fake.domain" },
                { "AW_TENANT_CODE", "AW_TENANT_CODE" },
                { "CERTIFICATE_PATH", "./test-certificate.p12" },
                { "DATA_EXTRACT_CONTAINER", "data-extract" },
                { "DATA_EXTRACT_FILE_NAME", "aw-users.json" },
                { "GWA_ETL_STORAGE_CONNECTION_STRING", "something-connection-string-ish" }
            };
            configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
        }

        private static Mock<HttpMessageHandler> SetUpHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Verifiable();
            return handlerMock;
        }

        private static IHttpClientFactory SetupHttpClientFactory(Mock<IHttpClientFactory> httpClientFactoryMock, Mock<HttpMessageHandler> handlerMock)
        {
            HttpClient httpClient = new(handlerMock.Object);
            _ = httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return httpClientFactoryMock.Object;
        }

        [Fact]
        public async void UnsuccessfulStatusCodeThrowsAndLogsError()
        {
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.BadRequest };
            Mock<HttpMessageHandler> handlerMock = SetUpHttpMessageHandler(responseMessage);
            IHttpClientFactory httpClientFactory = SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            _ = await Assert.ThrowsAsync<HttpRequestException>(() => extractAWData.Run(null));

            Verifiers.VerifyLogError(loggerMock, "Response status code does not indicate success: 400 (Bad Request).");
        }

        [Fact]
        public async void NoDevicesReturnedLogsInformation()
        {
            AirWatchApiResponse apiResponse = new() { Devices = new List<Device>() };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = SetUpHttpMessageHandler(responseMessage);
            IHttpClientFactory httpClientFactory = SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            await extractAWData.Run(null);

            Verifiers.VerifyLogInfo(loggerMock, "C# Timer trigger function executed at:");
            Verifiers.VerifyLogInfo(loggerMock, "Request - ");
            Verifiers.VerifyLogInfo(loggerMock, "Response - StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: System.Net.Http.StringContent");
            Verifiers.VerifyLogInfo(loggerMock, "DeviceCount: 0");
            Verifiers.VerifyLogInfo(loggerMock, "Page: 0\nPageSize: 0\nTotal: 0");
            Verifiers.VerifyLogInfoReport(loggerMock, new ReportLog() { DevicesProcessed = 0, DevicesWithNoPhoneNumber = 0, DevicesWithNoUserEmailAddress = 0, DevicesWithUserEmailAddress = 0, IPads = 0 });
        }

        [Fact]
        public async void DevicesAreProcessed()
        {
            IList<Device> devices = new List<Device>()
            {
                { new Device() { ModelId = new ModelId() { Id = new Id() { Value = 1 } }, PhoneNumber = "+447700111222", UserEmailAddress = "two-phone-numbers@gwa.com" } },
                { new Device() { ModelId = new ModelId() { Id = new Id() { Value = 1 } }, PhoneNumber = "+447700222333", UserEmailAddress = "two-phone-numbers@gwa.com" } },
                { new Device() { ModelId = new ModelId() { Id = new Id() { Value = 1 } }, PhoneNumber = "+447700333444", UserEmailAddress = "one-phone-number@gwa.com" } },
                { new Device() { ModelId = new ModelId() { Id = new Id() { Value = 1 } }, PhoneNumber = "", UserEmailAddress = "zero-phone-number@gwa.com" } },
                { new Device() { ModelId = new ModelId() { Id = new Id() { Value = 1 } }, PhoneNumber = "+447700111222", UserEmailAddress = "" } },
                { new Device() { ModelId = new ModelId() { Id = new Id() { Value = 2 } }, PhoneNumber = "+4477004445555", UserEmailAddress = "ipad@gwa.com" } }
            };
            AirWatchApiResponse apiResponse = new() { Devices = devices };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = SetUpHttpMessageHandler(responseMessage);
            IHttpClientFactory httpClientFactory = SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            await extractAWData.Run(null);

            Verifiers.VerifyLogInfoReport(loggerMock, new ReportLog() { DevicesProcessed = devices.Count, DevicesWithNoPhoneNumber = 1, DevicesWithNoUserEmailAddress = 1, DevicesWithUserEmailAddress = 3, IPads = 1 });

            blobClientMock.Verify(x => x.UploadAsync(It.IsAny<MemoryStream>(), true, It.IsAny<CancellationToken>()));
        }
    }
}
