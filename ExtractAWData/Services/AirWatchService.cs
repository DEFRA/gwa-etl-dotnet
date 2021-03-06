using Gwa.Etl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Gwa.Etl.Services
{
    public class AirWatchService
    {
        private readonly IConfiguration configuration;
        private readonly HttpClient httpClient;
        private readonly ILogger<ExtractAWData> logger;
        private readonly int pageSize = 500; // default is 500 prefer to be specific

        public AirWatchService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ExtractAWData> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            httpClient = httpClientFactory.CreateClient();
        }

        private static void AddPhoneNumberToExistingUser(User user, string phoneNumber)
        {
            if (!user.PhoneNumbers.Contains(phoneNumber))
            {
                user.PhoneNumbers.Add(phoneNumber);
            }
        }

        private static void AddUserWithPhoneNumber(IDictionary<string, User> users, User user, string emailAddress, string phoneNumber)
        {
            if (user == null)
            {
                user = new User { EmailAddress = emailAddress, PhoneNumbers = new List<string> { phoneNumber } };
                users.Add(emailAddress, user);
            }
            else
            {
                AddPhoneNumberToExistingUser(user, phoneNumber);
            }
        }

        private static void AddUserWithNoPhoneNumber(IDictionary<string, User> users, User user, string emailAddress)
        {
            if (user == null)
            {
                users.Add(emailAddress, new User { EmailAddress = emailAddress, PhoneNumbers = new List<string>() });
            }
        }

        private static int ProcessUserWithEmail(IDictionary<string, User> users, string emailAddress, string phoneNumber)
        {
            int noPhoneNumberCount = 0;
            _ = users.TryGetValue(emailAddress, out User user);
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                AddUserWithPhoneNumber(users, user, emailAddress, phoneNumber);
            }
            else
            {
                AddUserWithNoPhoneNumber(users, user, emailAddress);
                noPhoneNumberCount++;
            }
            return noPhoneNumberCount;
        }

        private static Tuple<int, int> ProcessUsers(IDictionary<string, User> users, Device device)
        {
            int noPhoneNumberCount = 0;
            int noEmailCount = 0;
            string phoneNumber = device.PhoneNumber;
            string emailAddress = device.UserEmailAddress?.ToLower(new CultureInfo("en-GB"));
            if (!string.IsNullOrWhiteSpace(emailAddress))
            {
                noPhoneNumberCount = ProcessUserWithEmail(users, emailAddress, phoneNumber);
            }
            else
            {
                noEmailCount++;
            }
            return Tuple.Create(noEmailCount, noPhoneNumberCount);
        }

        public async Task<ProcessedUsers> Process()
        {
            string certificatePath = configuration.GetValue<string>("CERTIFICATE_PATH");
            string awDomain = configuration.GetValue<string>("AW_DOMAIN");
            UriBuilder baseUri = new($"https://{awDomain}/api/mdm/devices/search");
            string authorizationHeader = new AuthorizationHeader(certificatePath).GetAuthHeader(baseUri.Path, DateTime.Now);
            string awTenantCode = configuration.GetValue<string>("AW_TENANT_CODE");

            int page = 0; // zero based
            bool morePages;
            int deviceCount = 0;
            int iPadCount = 0;
            int noEmailCount = 0;
            int noPhoneNumberCount = 0;

            Dictionary<string, User> users = new();
            do
            {
                baseUri.Query = $"page={page}&pagesize={pageSize}";
                page++;
                Uri uri = baseUri.Uri;

                HttpRequestMessage req = new(HttpMethod.Get, uri);
                req.Headers.Add("Authorization", authorizationHeader);
                req.Headers.Add("aw-tenant-code", awTenantCode);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage res = await httpClient.SendAsync(req);
                logger.LogInformation($"Request - {res.RequestMessage}");
                logger.LogInformation($"Response - {res}");
                _ = res.EnsureSuccessStatusCode();

                AirWatchApiResponse responseData = await res.Content.ReadFromJsonAsync<AirWatchApiResponse>();

                IList<Device> Devices = responseData.Devices;
                int Page = responseData.Page;
                int PageSize = responseData.PageSize;
                int Total = responseData.Total;
                int resDeviceCount = Devices.Count;
                logger.LogInformation($"Page: {Page}. PageSize: {PageSize}. DeviceCountOnPage: {resDeviceCount}. TotalDeviceCount: {Total}");

                for (int i = 0; i < resDeviceCount; i++)
                {
                    deviceCount++;
                    Device device = Devices[i];

                    if (device.ModelId.Id.Value == 2)
                    {
                        iPadCount++;
                    }
                    else
                    {
                        Tuple<int, int> counts = ProcessUsers(users, device);
                        noEmailCount += counts.Item1;
                        noPhoneNumberCount += counts.Item2;
                    }
                }
                logger.LogInformation($"Processed {deviceCount} devices.");
                morePages = page * pageSize < Total;
            } while (morePages);

            return new ProcessedUsers
            {
                Users = users,
                DeviceCount = deviceCount,
                IPadCount = iPadCount,
                NoEmailCount = noEmailCount,
                NoPhoneNumberCount = noPhoneNumberCount
            };
        }
    }
}
