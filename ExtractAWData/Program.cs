using Azure.Storage.Blobs;
using Gwa.Etl.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gwa.Etl
{
    public static class Program
    {
        public static void Main()
        {
            AirWatchBlobClientService airWatchBlobClientService = new();
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
