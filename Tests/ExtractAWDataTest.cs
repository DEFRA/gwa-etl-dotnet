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

        [Fact]
        public async void Test1()
        {
            AirWatchApiResponse apiResponse = new() { Devices = new List<Device>() };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) })
                .Verifiable();
            HttpClient httpClient = new(handlerMock.Object);
            _ = httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            ExtractAWData extractAWData = new(blobClientMock.Object, configuration, httpClientFactoryMock.Object, loggerMock.Object);
            await extractAWData.Run(null);

            loggerMock.Verify(logger => logger.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => o.ToString().StartsWith("C# Timer trigger function executed at: ", StringComparison.InvariantCulture)),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception, string>>()
                        ));
        }
    }
}
