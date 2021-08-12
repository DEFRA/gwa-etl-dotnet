using Azure.Storage.Blobs;
using Gwa.Etl.Services;
using System;
using Xunit;

namespace Gwa.Etl.Tests.Services
{
    public class AirWatchBlobClientServiceTests
    {
        [Fact]
        public void BlobClientIsCreatedWithCorrectEnvVars()
        {
            string blobEndpoint = "http://127.0.0.1:10000/devstoreaccount1";
            string container = "data-extract";
            string filename = "aw-users.json";

            Environment.SetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", $"AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint={blobEndpoint};");
            Environment.SetEnvironmentVariable("DATA_EXTRACT_CONTAINER", container);
            Environment.SetEnvironmentVariable("DATA_EXTRACT_FILE_NAME", filename);

            AirWatchBlobClientService blobClientService = new();

            BlobClient blobClient = blobClientService.CreateBlobClient();

            Assert.Equal($"{blobEndpoint}/{container}/{filename}", blobClient.Uri.ToString());
        }
    }
}
