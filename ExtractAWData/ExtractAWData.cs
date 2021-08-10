using Azure.Storage.Blobs;
using Gwa.Etl.Helpers;
using Gwa.Etl.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Gwa.Etl
{
    public class ExtractAWData
    {
        private readonly BlobClient blobClient;
        private readonly IConfiguration configuration;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ExtractAWData> logger;

        public ExtractAWData(BlobClient blobClient, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ExtractAWData> logger)
        {
            this.blobClient = blobClient;
            this.configuration = configuration;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
        }

        [Function("ExtractAWData")]
        public async Task Run([TimerTrigger("0 0 8 * * 0")] MyInfo myTimer)
        {
            try
            {
                logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                ProcessedUsers processedUsers = await GetUsers.Process(configuration, httpClientFactory, logger);

                IDictionary<string, User> users = processedUsers.Users;
                using (MemoryStream json = new(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(users.Values))))
                {
                    _ = await blobClient.UploadAsync(json, true);
                }

                logger.LogInformation($"Data extract from AW is complete.\n{processedUsers.DeviceCount} devices have been processed.");
                logger.LogInformation($"{users.Count} devices have a UserEmailAddress of which {processedUsers.NoPhoneNumberCount} have no PhoneNumber.");
                logger.LogInformation($"{processedUsers.NoEmailCount} devices with no UserEmailAddress.");
                logger.LogInformation($"{processedUsers.IPadCount} iPads have been ignored.");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}
