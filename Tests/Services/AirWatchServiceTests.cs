using Gwa.Etl.Models;
using Gwa.Etl.Services;
using Gwa.Etl.Tests.Helpers;
using Gwa.Etl.Tests.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Gwa.Etl.Tests.Services
{
    public class AirWatchServiceTests
    {
        private readonly IConfiguration configuration;
        private readonly Mock<IHttpClientFactory> httpClientFactoryMock = new();
        private readonly Mock<ILogger<ExtractAWData>> loggerMock = new();
        private readonly string awDomain = "fake.domain";
        private readonly string awTenantCode = "AW_TENANT_CODE";

        public AirWatchServiceTests()
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
        }

        private AirWatchServiceTestSetup SetupAirWatchService(IList<Device> devices)
        {
            AirWatchApiResponse apiResponse = new() { Devices = devices };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage, awDomain, awTenantCode);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);
            return new AirWatchServiceTestSetup()
            {
                AirWatchService = new(configuration, httpClientFactory, loggerMock.Object),
                HandlerMock = handlerMock
            };
        }

        [Fact]
        public async void UnsuccessfulStatusCodeThrows()
        {
            HttpResponseMessage responseMessage = new() { StatusCode = HttpStatusCode.BadRequest };
            Mock<HttpMessageHandler> handlerMock = HttpSetup.SetUpHttpMessageHandler(responseMessage, awDomain, awTenantCode);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            AirWatchService airWatchService = new(configuration, httpClientFactory, loggerMock.Object);
            _ = await Assert.ThrowsAsync<HttpRequestException>(() => airWatchService.Process());
        }

        [Fact]
        public async void NoDevicesReturnedReturnsNoDevices()
        {
            IList<Device> devices = new List<Device>();
            AirWatchServiceTestSetup testSetup = SetupAirWatchService(devices);

            ProcessedUsers processedUsers = await testSetup.AirWatchService.Process();

            Assert.Equal(0, processedUsers.DeviceCount);
            Assert.Equal(0, processedUsers.IPadCount);
            Assert.Equal(0, processedUsers.NoEmailCount);
            Assert.Equal(0, processedUsers.NoPhoneNumberCount);
            Assert.Equal(new Dictionary<string, User>(), processedUsers.Users);
        }

        private static Device CreateDevice(int modelId, string phoneNumber, string emailAddress)
        {
            return new Device() { ModelId = new ModelId() { Id = new Id() { Value = modelId } }, PhoneNumber = phoneNumber, UserEmailAddress = emailAddress };
        }

        [Fact]
        public async void IPadsAreIgnored()
        {
            string emailAddress = "user@gwa.com";
            IList<Device> devices = new List<Device>()
            {
                { CreateDevice(2, "+447700111222", emailAddress) }
            };
            AirWatchServiceTestSetup testSetup = SetupAirWatchService(devices);

            ProcessedUsers processedUsers = await testSetup.AirWatchService.Process();

            Assert.Equal(devices.Count, processedUsers.DeviceCount);
            Assert.Equal(1, processedUsers.IPadCount);
            Assert.Equal(0, processedUsers.NoEmailCount);
            Assert.Equal(0, processedUsers.NoPhoneNumberCount);
            Assert.Equal(0, processedUsers.Users.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async void DevicesWithNoEmailAddressAreCountedButNotReturned(string emailAddress)
        {
            IList<Device> devices = new List<Device>()
            {
                { CreateDevice(1, "+447700222333", emailAddress) }
            };
            AirWatchServiceTestSetup testSetup = SetupAirWatchService(devices);

            ProcessedUsers processedUsers = await testSetup.AirWatchService.Process();

            Assert.Equal(devices.Count, processedUsers.DeviceCount);
            Assert.Equal(0, processedUsers.IPadCount);
            Assert.Equal(1, processedUsers.NoEmailCount);
            Assert.Equal(0, processedUsers.NoPhoneNumberCount);
            Assert.Equal(0, processedUsers.Users.Count);
        }

        [Fact]
        public async void UsersWithMultiplePhoneNumbersAreReturnedAsSuch()
        {
            string emailAddress = "user@gwa.com";
            IList<Device> devices = new List<Device>()
            {
                { CreateDevice(1, "+447700111222", emailAddress) },
                { CreateDevice(1, "+447700222333", emailAddress) }
            };
            AirWatchServiceTestSetup testSetup = SetupAirWatchService(devices);

            ProcessedUsers processedUsers = await testSetup.AirWatchService.Process();

            Assert.Equal(devices.Count, processedUsers.DeviceCount);
            Assert.Equal(0, processedUsers.IPadCount);
            Assert.Equal(0, processedUsers.NoEmailCount);
            Assert.Equal(0, processedUsers.NoPhoneNumberCount);
            Assert.Equal(1, processedUsers.Users.Count);
            Assert.Equal(emailAddress, processedUsers.Users[emailAddress].EmailAddress);
            Assert.Equal(2, processedUsers.Users[emailAddress].PhoneNumbers.Count);
            Assert.Equal(devices[0].PhoneNumber, processedUsers.Users[emailAddress].PhoneNumbers[0]);
            Assert.Equal(devices[1].PhoneNumber, processedUsers.Users[emailAddress].PhoneNumbers[1]);
        }

        [Fact]
        public async void UsersWithNoPhoneNumbersAreReturnedAsSuch()
        {
            string emailAddress = "user@gwa.com";
            IList<Device> devices = new List<Device>()
            {
                { CreateDevice(1, "", emailAddress) }
            };
            AirWatchServiceTestSetup testSetup = SetupAirWatchService(devices);

            ProcessedUsers processedUsers = await testSetup.AirWatchService.Process();

            Assert.Equal(devices.Count, processedUsers.DeviceCount);
            Assert.Equal(0, processedUsers.IPadCount);
            Assert.Equal(0, processedUsers.NoEmailCount);
            Assert.Equal(1, processedUsers.NoPhoneNumberCount);
            Assert.Equal(1, processedUsers.Users.Count);
            Assert.Equal(emailAddress, processedUsers.Users[emailAddress].EmailAddress);
            Assert.Equal(0, processedUsers.Users[emailAddress].PhoneNumbers.Count);
            Assert.Equal(new List<string>(), processedUsers.Users[emailAddress].PhoneNumbers);
        }

        [Theory]
        [InlineData("UPPER@GWA.COM")]
        [InlineData("lower@gwa.com")]
        [InlineData("MiXeD@GwA.cOm")]
        public async void EmailAddressesAreLowerCased(string emailAddress)
        {
            string lowerEmail = emailAddress.ToLower(new CultureInfo("en-GB"));
            IList<Device> devices = new List<Device>()
            {
                { CreateDevice(1, "+447700111222",emailAddress) }
            };
            AirWatchServiceTestSetup testSetup = SetupAirWatchService(devices);

            ProcessedUsers processedUsers = await testSetup.AirWatchService.Process();

            Assert.Equal(devices.Count, processedUsers.DeviceCount);
            Assert.Equal(0, processedUsers.IPadCount);
            Assert.Equal(0, processedUsers.NoEmailCount);
            Assert.Equal(0, processedUsers.NoPhoneNumberCount);
            Assert.Equal(devices.Count, processedUsers.Users.Count);
            Assert.Equal(lowerEmail, processedUsers.Users[lowerEmail].EmailAddress);
        }

        [Fact]
        public async void Over500DevicesReturnedResultsInAdditionalRequests()
        {
            string emailAddress = "test@gwa.com";
            IList<Device> devices = new List<Device>()
            {
                { CreateDevice(1, "+447700111222", emailAddress) }
            };
            AirWatchApiResponse apiResponse = new() { Devices = devices, Total = 501 };
            JObject json = (JObject)JToken.FromObject(apiResponse);
            HttpResponseMessage firstResponse = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            HttpResponseMessage secondResponse = new() { StatusCode = HttpStatusCode.OK, Content = new StringContent(json.ToString()) };
            Mock<HttpMessageHandler> handlerMock = new(MockBehavior.Strict);
            _ = handlerMock
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.Is<HttpRequestMessage>(
                            x => x.Method == HttpMethod.Get
                            && x.RequestUri.ToString().StartsWith($"https://{awDomain}/api/mdm/devices/search?page=", StringComparison.Ordinal)
                            && x.Headers.Accept.ToString() == new MediaTypeWithQualityHeaderValue("application/json").ToString()
                            && x.Headers.GetValues("aw-tenant-code").First() == awTenantCode
                            ),
                        ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(firstResponse)
                .ReturnsAsync(secondResponse);
            IHttpClientFactory httpClientFactory = HttpSetup.SetupHttpClientFactory(httpClientFactoryMock, handlerMock);

            AirWatchService airWatchService = new(configuration, httpClientFactory, loggerMock.Object);
            ProcessedUsers processedUsers = await airWatchService.Process();

            Assert.Equal(2, processedUsers.DeviceCount);
            Assert.Equal(0, processedUsers.IPadCount);
            Assert.Equal(0, processedUsers.NoEmailCount);
            Assert.Equal(0, processedUsers.NoPhoneNumberCount);
            Assert.Equal(1, processedUsers.Users.Count);
            Assert.Equal(emailAddress, processedUsers.Users[emailAddress].EmailAddress);
            Assert.Equal(1, processedUsers.Users[emailAddress].PhoneNumbers.Count);
            Assert.Equal(devices[0].PhoneNumber, processedUsers.Users[emailAddress].PhoneNumbers[0]);
        }
    }
}
