using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.IO;
using System.Text;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Functions.Worker;
using System.Globalization;

namespace Defra.Gwa.Etl
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
    public class AirWatchApiResponse
    {
        public IList<Device> Devices { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }
        public bool IsPastDue { get; set; }
    }
    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }

    public static class ExtractAWData
    {
        private static readonly HttpClient client = new();
        private static readonly string awDomain = Environment.GetEnvironmentVariable("AW_DOMAIN", EnvironmentVariableTarget.Process);
        private static readonly string awTenantCode = Environment.GetEnvironmentVariable("AW_TENANT_CODE", EnvironmentVariableTarget.Process);
        private static readonly string connectionString = Environment.GetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
        private static readonly string dataExtractContainer = Environment.GetEnvironmentVariable("DATA_EXTRACT_CONTAINER", EnvironmentVariableTarget.Process);
        private static readonly string dataExtractFileName = Environment.GetEnvironmentVariable("DATA_EXTRACT_FILE_NAME", EnvironmentVariableTarget.Process);

        private static string GetAuthHeader(UriBuilder baseUri)
        {
            string certificatePath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH", EnvironmentVariableTarget.Process);
            X509Certificate2 certificate = new(certificatePath);
            CmsSigner signer = new(certificate);
            _ = signer.SignedAttributes.Add(new Pkcs9SigningTime());
            byte[] signingData = Encoding.UTF8.GetBytes(baseUri.Path);
            SignedCms signedCms = new(new ContentInfo(signingData), detached: true);
            signedCms.ComputeSignature(signer);
            byte[] signature = signedCms.Encode();
            return $"CMSURL`1 {Convert.ToBase64String(signature)}";
        }

        [Function("ExtractAWData")]
        public static async Task Run(
            [TimerTrigger("0 0 8 * * 0")] MyInfo myTimer, FunctionContext context)
        {
            ILogger logger = context.GetLogger("ExtractAWData");
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            int pageSize = 500; // default is 500 prefer to be specific
            int page = 0; // zero based
            bool next;
            int deviceCount = 0;
            int iPadCount = 0;
            int noEmailCount = 0;
            int noPhoneNumberCount = 0;

            UriBuilder baseUri = new($"https://{awDomain}/api/mdm/devices/search");

            string authorizationHeader = GetAuthHeader(baseUri);

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

                HttpResponseMessage res = await client.SendAsync(req);
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
                next = page * pageSize < Total;
            } while (next);

            BlobServiceClient serviceClient = new(connectionString);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(dataExtractContainer);
            BlobClient blobClient = containerClient.GetBlobClient(dataExtractFileName);
            using (MemoryStream json = new(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(users.Values))))
            {
                _ = await blobClient.UploadAsync(json, true);
            }

            logger.LogInformation($"Data extract from AW is complete.\n{deviceCount} devices have been processed.");
            logger.LogInformation($"{users.Count} devices have a UserEmailAddress of which {noPhoneNumberCount} have no PhoneNumber.");
            logger.LogInformation($"{noEmailCount} devices with no UserEmailAddress.");
            logger.LogInformation($"{iPadCount} iPads have been ignored.");
        }
    }
}
