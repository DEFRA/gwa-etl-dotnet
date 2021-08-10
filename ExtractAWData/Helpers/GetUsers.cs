using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Gwa.Etl.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gwa.Etl.Helpers
{
    public class GetUsers
    {
        public static async Task<ProcessedUsers> Process(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            string certificatePath = configuration.GetValue<string>("CERTIFICATE_PATH");
            string awDomain = configuration.GetValue<string>("AW_DOMAIN");
            UriBuilder baseUri = new($"https://{awDomain}/api/mdm/devices/search");
            string authorizationHeader = Authorization.GetAuthHeader(baseUri.Path, certificatePath);
            string awTenantCode = configuration.GetValue<string>("AW_TENANT_CODE");

            int pageSize = 500; // default is 500 prefer to be specific
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

                HttpClient httpClient = httpClientFactory.CreateClient();
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
                logger.LogInformation($"DeviceCount: {resDeviceCount}");
                logger.LogInformation($"Page: {Page}\nPageSize: {PageSize}\nTotal: {Total}");

                for (int i = 0; i < resDeviceCount; i++)
                {
                    deviceCount++;
                    Device device = Devices[i];
                    ModelId ModelId = device.ModelId;
                    string phoneNumber = device.PhoneNumber;
                    string emailAddress = device.UserEmailAddress.ToLower(new CultureInfo("en-GB"));

                    if (ModelId.Id.Value == 2)
                    {
                        iPadCount++;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(emailAddress))
                        {
                            _ = users.TryGetValue(emailAddress, out User user);
                            if (!string.IsNullOrWhiteSpace(phoneNumber))
                            {
                                if (user != null)
                                {
                                    if (!user.PhoneNumbers.Contains(phoneNumber))
                                    {
                                        user.PhoneNumbers.Add(phoneNumber);
                                    }
                                }
                                else
                                {
                                    user = new User { EmailAddress = emailAddress, PhoneNumbers = new List<string> { phoneNumber } };
                                    users.Add(emailAddress, user);
                                }
                            }
                            else
                            {
                                if (user == null)
                                {
                                    users.Add(emailAddress, new User { EmailAddress = emailAddress, PhoneNumbers = new List<string>() });
                                }
                                noPhoneNumberCount++;
                            }
                        }
                        else
                        {
                            noEmailCount++;
                        }
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
