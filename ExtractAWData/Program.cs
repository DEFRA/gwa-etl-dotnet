using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gwa.Etl
{
    public static class Program
    {
        public static void Main()
        {
            string connectionString = Environment.GetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            string dataExtractContainer = Environment.GetEnvironmentVariable("DATA_EXTRACT_CONTAINER", EnvironmentVariableTarget.Process);
            string dataExtractFileName = Environment.GetEnvironmentVariable("DATA_EXTRACT_FILE_NAME", EnvironmentVariableTarget.Process);

            BlobServiceClient serviceClient = new(connectionString);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(dataExtractContainer);
            BlobClient blobClient = containerClient.GetBlobClient(dataExtractFileName);

            IHost host = new HostBuilder()
              .ConfigureFunctionsWorkerDefaults()
              .ConfigureServices(s => { _ = s.AddHttpClient(); _ = s.AddSingleton(blobClient); })
              .ConfigureHostConfiguration(config => { _ = config.AddJsonFile("local.settings.json", true, true); _ = config.AddEnvironmentVariables(); })
              .Build();

            host.Run();
        }
    }
}
