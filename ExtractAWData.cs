using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.IO;
using System.Text;

namespace gwa_etl_dotnet
{
    public class User
    {
        [JsonProperty("emailAddress")]
        public string EmailAddress { get; set; }
        [JsonProperty("phoneNumbers")]
        public IList<string> PhoneNumbers { get; set; }
    }
    public class Id
    {
        public int Value { get; set; }
    }
    public class ModelId
    {
        public Id Id { get; set; }
    }
    public class Device
    {
        public string PhoneNumber { get; set; }
        public string UserEmailAddress { get; set; }
        public ModelId ModelId { get; set; }
    }
    public class AirWatchAPIResponse
    {
        public IList<Device> Devices { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public static class ExtractAWData
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string awAuthHeader = Environment.GetEnvironmentVariable("AW_AUTH_HEADER", EnvironmentVariableTarget.Process);
        private static readonly string awDomain = Environment.GetEnvironmentVariable("AW_DOMAIN", EnvironmentVariableTarget.Process);
        private static readonly string awFileName = Environment.GetEnvironmentVariable("AW_FILE_NAME", EnvironmentVariableTarget.Process);
        private static readonly string awTenantCode = Environment.GetEnvironmentVariable("AW_TENANT_CODE", EnvironmentVariableTarget.Process);
        private static readonly string container = Environment.GetEnvironmentVariable("DATA_EXTRACT_CONTAINER", EnvironmentVariableTarget.Process);
        private static readonly string connectionString = Environment.GetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);

        [FunctionName("ExtractAWData")]
        public static async Task Run(
            [TimerTrigger("0 0 8 * * 0")] TimerInfo myTimer,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            Dictionary<string, User> users = new Dictionary<string, User>();
            int pageSize = 5; // default is 500 prefer to be specific
            int page = 0; // zero based
            bool next = false;
            int deviceCount = 0;
            int iPadCount = 0;
            int noEmailCount = 0;
            int noPhoneNumberCount = 0;

            do
            {
                string url = $"https://{awDomain}/API/mdm/devices/search?pagesize={pageSize}&page={page}";
                page++;
                log.LogInformation($"Request URL: {url}.");
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("Authorization", awAuthHeader);
                req.Headers.Add("aw-tenant-code", awTenantCode);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage rr = await client.SendAsync(req);
                log.LogInformation($"Response\nStatus: {rr.StatusCode}\nHeaders:\n{rr.Headers}");
                rr.EnsureSuccessStatusCode();
                AirWatchAPIResponse res = await rr.Content.ReadFromJsonAsync<AirWatchAPIResponse>();

                /* log.LogInformation(JsonConvert.SerializeObject(res)); */

                IList<Device> Devices = res.Devices;
                int Page = res.Page;
                int PageSize = res.PageSize;
                int Total = res.Total;
                int resDeviceCount = Devices.Count;
                log.LogInformation($"DeviceCount: {resDeviceCount}");
                log.LogInformation($"Page: {Page}\nPageSize: {PageSize}\nTotal: {Total}");

                for (int i = 0; i < resDeviceCount; i++)
                {
                    deviceCount++;
                    Device device = Devices[i];
                    ModelId ModelId = device.ModelId;
                    string phoneNumber = device.PhoneNumber;
                    string emailAddress = device.UserEmailAddress.ToLower();

                    if (ModelId.Id.Value == 2)
                    {
                        iPadCount++;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(emailAddress))
                        {
                            users.TryGetValue(emailAddress, out User user);
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
                log.LogInformation($"Processed {deviceCount} devices.");
                /* next = page * pageSize < Total; */
            } while (next);

            /* log.LogInformation(JsonConvert.SerializeObject(users.Values)); */
            BlobServiceClient bsc = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = bsc.GetBlobContainerClient(container);

            BlobClient blobClient = containerClient.GetBlobClient(awFileName);
            MemoryStream json = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(users.Values)));
            await blobClient.UploadAsync(json, true);

            log.LogInformation($"Data extract from AW is complete.\n{deviceCount} devices have been processed.");
            log.LogInformation($"{users.Count} devices have a UserEmailAddress of which {noPhoneNumberCount} have no PhoneNumber.");
            log.LogInformation($"{noEmailCount} devices with no UserEmailAddress.");
            log.LogInformation($"{iPadCount} iPads have been ignored.");
        }
    }
}
