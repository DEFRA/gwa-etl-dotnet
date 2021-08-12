using Azure.Storage.Blobs;
using Gwa.Etl.Models;
using Gwa.Etl.Tests.Helpers;
using Gwa.Etl.Tests.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gwa.Etl.Tests
{
    public class ExtractAWDataTests
    {
        private readonly IConfiguration configuration;
        private readonly Mock<BlobClient> blobClientMock = new();
        private readonly Mock<IHttpClientFactory> httpClientFactoryMock = new();
        private readonly Mock<ILogger<ExtractAWData>> loggerMock = new();
        private readonly string awDomain = "fake.domain";
        private readonly string awTenantCode = "AW_TENANT_CODE";
        private readonly TimerInfo timerInfo;

        public ExtractAWDataTests()
        {
            IDictionary<string, string> config = new Dictionary<string, string>()
            {
                { "AW_DOMAIN", awDomain },
                { "AW_TENANT_CODE", awTenantCode },
                { "CERTIFICATE_PATH", "./test-certificate.p12" },
                { "DATA_EXTRACT_CONTAINER", "data-extract" },
                { "DATA_EXTRACT_FILE_NAME", "aw-users.json" },
                { "GWA_ETL_STORAGE_CONNECTION_STRING", "something-connection-string-ish" }
            };
            configuration = new ConfigurationBuilder().AddInMemoryCollection(config).Build();

            DateTime now = DateTime.Now;
            timerInfo = new()
            {
                ScheduleStatus = new()
                {
                    Last = now.AddDays(-7),
                    LastUpdated = now,
                    Next = now.AddDays(7)
                }
            };
        }

        [Fact]
        public async Task UnsuccessfulStatusCodeThrowsAndLogsError()
        {
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.BadRequest };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage, awDomain, awTenantCode);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            _ = await Assert.ThrowsAsync<HttpRequestException>(() => extractAWData.Run(timerInfo));

            Verifiers.VerifyLogError(loggerMock, "Response status code does not indicate success: 400 (Bad Request).");
        }

        [Fact]
        public async Task NoDevicesReturnedLogsInformation()
        {
            AirWatchApiResponse apiResponse = new() { Devices = new List<Device>(), Page = 0, PageSize = 500, Total = 456 };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage, awDomain, awTenantCode);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            await extractAWData.Run(timerInfo);

            Verifiers.VerifyLogInfo(loggerMock, $"C# Timer trigger function executed at: ");
            Verifiers.VerifyLogInfo(loggerMock, $"Last execution was at {timerInfo.ScheduleStatus.Last}. Next execution will be at {timerInfo.ScheduleStatus.Next}");
            Verifiers.VerifyLogInfo(loggerMock, "Request - ");
            Verifiers.VerifyLogInfo(loggerMock, "Response - StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: System.Net.Http.StringContent");
            Verifiers.VerifyLogInfo(loggerMock, $"Page: {apiResponse.Page}. PageSize: {apiResponse.PageSize}. DeviceCountOnPage: {apiResponse.Devices.Count}. TotalDeviceCount: {apiResponse.Total}");
            Verifiers.VerifyLogInfoReport(loggerMock, new ReportLog() { DevicesProcessed = 0, DevicesWithNoPhoneNumber = 0, DevicesWithNoUserEmailAddress = 0, DevicesWithUserEmailAddress = 0, IPads = 0 });
        }

        [Fact]
        public async Task DevicesReturnedLogsInformationAndUploadsData()
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
            AirWatchApiResponse apiResponse = new() { Devices = devices, Page = 0, PageSize = 500, Total = 123 };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage, awDomain, awTenantCode);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            await extractAWData.Run(timerInfo);

            Verifiers.VerifyLogInfo(loggerMock, $"Page: {apiResponse.Page}. PageSize: {apiResponse.PageSize}. DeviceCountOnPage: {apiResponse.Devices.Count}. TotalDeviceCount: {apiResponse.Total}");
            Verifiers.VerifyLogInfoReport(loggerMock, new ReportLog() { DevicesProcessed = devices.Count, DevicesWithNoPhoneNumber = 1, DevicesWithNoUserEmailAddress = 1, DevicesWithUserEmailAddress = 3, IPads = 1 });

            blobClientMock.Verify(x => x.UploadAsync(It.IsAny<MemoryStream>(), true, It.IsAny<CancellationToken>()));
        }
    }
}
