using Moq;
using Moq.Protected;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Gwa.Etl.Tests.Helpers
{
    public class HttpSetup
    {
        public static Mock<HttpMessageHandler> SetUpHttpMessageHandler(HttpResponseMessage responseMessage, string awDomain, string awTenantCode)
        {
            Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.Is<HttpRequestMessage>(
                            x => x.Method == HttpMethod.Get
                            && x.RequestUri.ToString().StartsWith($"https://{awDomain}/api/mdm/devices/search?page=0&pagesize=500", StringComparison.Ordinal)
                            && x.Headers.Accept.ToString() == new MediaTypeWithQualityHeaderValue("application/json").ToString()
                            && x.Headers.GetValues("aw-tenant-code").First() == awTenantCode
                            ),
                        ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage)
                .Verifiable();
            return handlerMock;
        }

        public static IHttpClientFactory SetupHttpClientFactory(Mock<IHttpClientFactory> httpClientFactoryMock, Mock<HttpMessageHandler> handlerMock)
        {
            HttpClient httpClient = new(handlerMock.Object);
            _ = httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            return httpClientFactoryMock.Object;
        }
    }
}
