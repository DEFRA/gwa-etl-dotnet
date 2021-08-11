using Azure.Storage.Blobs;
using Gwa.Etl.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Gwa.Etl
{
    public static class Program
    {
        public static void Main()
        {
            string connectionString = Environment.GetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            string dataExtractContainer = Environment.GetEnvironmentVariable("DATA_EXTRACT_CONTAINER", EnvironmentVariableTarget.Process);
            string dataExtractFileName = Environment.GetEnvironmentVariable("DATA_EXTRACT_FILE_NAME", EnvironmentVariableTarget.Process);

            AirWatchBlobClientService airWatchBlobClientService = new(connectionString, dataExtractContainer, dataExtractFileName);
            BlobClient blobClient = airWatchBlobClientService.CreateBlobClient();

            IHost host = new HostBuilder()
              .ConfigureFunctionsWorkerDefaults()
              .ConfigureServices(s => { _ = s.AddHttpClient(); _ = s.AddSingleton(blobClient); })
              .ConfigureHostConfiguration(config => { _ = config.AddJsonFile("local.settings.json", true, true); _ = config.AddEnvironmentVariables(); })
              .Build();

            host.Run();
        }
    }
}
