using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace Gwa.Etl.Tests.Helpers
{
    public class HttpSetup
    {
        public static Mock<HttpMessageHandler> SetUpHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
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
