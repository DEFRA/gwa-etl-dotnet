using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Defra.Gwa.Etl
{
    public class ExtractAWData
    {
        private readonly string authorizationHeader;
        private readonly string awTenantCode;
        private readonly UriBuilder baseUri;
        private readonly BlobClient blobClient;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ExtractAWData> logger;

        public ExtractAWData(BlobClient blobClient, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ExtractAWData> logger)
        {
            awTenantCode = configuration.GetValue<string>("AW_TENANT_CODE");
            this.blobClient = blobClient;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;


            string awDomain = configuration.GetValue<string>("AW_DOMAIN");
            string certificatePath = configuration.GetValue<string>("CERTIFICATE_PATH");
            baseUri = new($"https://{awDomain}/api/mdm/devices/search");
            authorizationHeader = GetAuthHeader(baseUri.Path, certificatePath);
        }

        private static string GetAuthHeader(string path, string certificatePath)
        {
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
            try
            {
                logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

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

                using (MemoryStream json = new(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(users.Values))))
                {
                    _ = await blobClient.UploadAsync(json, true);
                }

                logger.LogInformation($"Data extract from AW is complete.\n{deviceCount} devices have been processed.");
                logger.LogInformation($"{users.Count} devices have a UserEmailAddress of which {noPhoneNumberCount} have no PhoneNumber.");
                logger.LogInformation($"{noEmailCount} devices with no UserEmailAddress.");
                logger.LogInformation($"{iPadCount} iPads have been ignored.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}