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
using Microsoft.Extensions.Configuration;

namespace Defra.Gwa.Etl
{
    public class ExtractAWData
    {
        private static readonly string connectionString = Environment.GetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
        private static readonly string dataExtractContainer = Environment.GetEnvironmentVariable("DATA_EXTRACT_CONTAINER", EnvironmentVariableTarget.Process);
        private static readonly string dataExtractFileName = Environment.GetEnvironmentVariable("DATA_EXTRACT_FILE_NAME", EnvironmentVariableTarget.Process);

        private readonly string authorizationHeader;
        private readonly string awTenantCode;
        private readonly UriBuilder baseUri;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ExtractAWData> logger;

        public ExtractAWData(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ExtractAWData> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;

            awTenantCode = configuration.GetValue<string>("AW_TENANT_CODE");

            string awDomain = configuration.GetValue<string>("AW_DOMAIN");
            baseUri = new($"https://{awDomain}/api/mdm/devices/search");
            authorizationHeader = GetAuthHeader(baseUri.Path);
        }

        private static string GetAuthHeader(string path)
        {
            string certificatePath = Environment.GetEnvironmentVariable("CERTIFICATE_PATH", EnvironmentVariableTarget.Process);
            X509Certificate2 certificate = new(certificatePath);
            CmsSigner signer = new(certificate);
            _ = signer.SignedAttributes.Add(new Pkcs9SigningTime());
            byte[] signingData = Encoding.UTF8.GetBytes(path);
            SignedCms signedCms = new(new ContentInfo(signingData), detached: true);
            signedCms.ComputeSignature(signer);
            byte[] signature = signedCms.Encode();
            return $"CMSURL`1 {Convert.ToBase64String(signature)}";
        }

        [Function("ExtractAWData")]
        public async Task Run([TimerTrigger("0 0 8 * * 0")] MyInfo myTimer)
        {
            logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            int pageSize = 500; // default is 500 prefer to be specific
            int page = 0; // zero based
            bool next;
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
