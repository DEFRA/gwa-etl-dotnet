using Gwa.Etl.Models;
using Gwa.Etl.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Xunit;

namespace Gwa.Etl.Tests.Helpers
{
    public class AirWatchServiceTests
    {
        private readonly IConfiguration configuration;
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

        [Fact]
        public async void UnsuccessfulStatusCodeThrows()
        {
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.BadRequest };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            AirWatchService airWatchService = new(configuration, httpClientFactory, loggerMock.Object);
            _ = await Assert.ThrowsAsync<HttpRequestException>(() => airWatchService.Process());
        }

        [Fact]
        public async void NoDevicesReturnedReturnsNoDevices()
        {
            AirWatchApiResponse apiResponse = new() { Devices = new List<Device>() };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            AirWatchService airWatchService = new(configuration, httpClientFactory, loggerMock.Object);
            ProcessedUsers processedUsers = await airWatchService.Process();
        }

        [Fact]
        public async void DevicesAreProcessedAndReturned()
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
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            AirWatchService airWatchService = new(configuration, httpClientFactory, loggerMock.Object);
            ProcessedUsers processedUsers = await airWatchService.Process();
        }
    }
}
