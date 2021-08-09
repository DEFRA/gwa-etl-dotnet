using Azure.Storage.Blobs;
using Defra.Gwa.Etl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ExtractAWDataTests
{
    public class ExtractAWDataTest
    {
        private readonly IConfiguration configuration;
        private readonly Mock<BlobClient> blobClientMock = new();
        private readonly Mock<IHttpClientFactory> httpClientFactoryMock = new();
        private readonly Mock<ILogger<ExtractAWData>> loggerMock = new();

        public ExtractAWDataTest()
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

        private static IHttpClientFactory SetupHttpClientFactory(Mock<IHttpClientFactory> httpClientFactoryMock, HttpResponseMessage responseMessage)
        {
            Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Verifiable();
            HttpClient httpClient = new(handlerMock.Object);
            _ = httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return httpClientFactoryMock.Object;
        }

        private static IHttpClientFactory SetupHttpClientFactoryForFailure(Mock<IHttpClientFactory> httpClientFactoryMock)
        {
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.BadRequest };
            return SetupHttpClientFactory(httpClientFactoryMock, responseMessage);
        }

        private static IHttpClientFactory SetupHttpClientFactoryForSuccess(Mock<IHttpClientFactory> httpClientFactoryMock, AirWatchApiResponse apiResponse)
        {
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            return SetupHttpClientFactory(httpClientFactoryMock, responseMessage);
        }

        private static void VerifyLogError(Mock<ILogger<ExtractAWData>> loggerMock, string message)
        {
            loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ));
        }

        private static void VerifyLogInformation(Mock<ILogger<ExtractAWData>> loggerMock, string message)
        {
            loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ));
        }

        [Fact]
        public async void UnsuccessfulStatusCodeThrowsAndLogsError()
        {
            IHttpClientFactory httpClientFactory = SetupHttpClientFactoryForFailure(httpClientFactoryMock);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            _ = await Assert.ThrowsAsync<HttpRequestException>(() => extractAWData.Run(null));

            VerifyLogError(loggerMock, "Response status code does not indicate success: 400 (Bad Request).");
        }

        [Fact]
        public async void NoDevicesReturnedLogsInformation()
        {
            AirWatchApiResponse apiResponse = new() { Devices = new List<Device>() };
            IHttpClientFactory httpClientFactory = SetupHttpClientFactoryForSuccess(httpClientFactoryMock, apiResponse);

            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactory, loggerMock.Object);
            await extractAWData.Run(null);

            VerifyLogInformation(loggerMock, "C# Timer trigger function executed at:");
            VerifyLogInformation(loggerMock, "Request - ");
            VerifyLogInformation(loggerMock, "Response - StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: System.Net.Http.StringContent");
            VerifyLogInformation(loggerMock, "DeviceCount: 0");
            VerifyLogInformation(loggerMock, "Page: 0\nPageSize: 0\nTotal: 0");
            VerifyLogInformation(loggerMock, "Data extract from AW is complete.\n0 devices have been processed.");
            VerifyLogInformation(loggerMock, "0 devices have a UserEmailAddress of which 0 have no PhoneNumber.");
            VerifyLogInformation(loggerMock, "0 devices with no UserEmailAddress.");
            VerifyLogInformation(loggerMock, "0 iPads have been ignored.");
        }
    }
}
