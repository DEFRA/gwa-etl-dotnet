using Azure.Storage.Blobs;
using Gwa.Etl.Services;
using Xunit;

namespace Gwa.Etl.Tests.Services
{
    public class AirWatchBlobClientServiceTests
    {
        [Fact]
        public void AuthorizationHeaderIsCorrect()
        {
            string connectionString = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;DefaultEndpointsProtocol=http;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
            string container = "data-extract";
            string filename = "aw-users.json";

            AirWatchBlobClientService blobClientService = new(connectionString, container, filename);

            BlobClient blobClient = blobClientService.CreateBlobClient();

            Assert.Equal($"http://127.0.0.1:10000/devstoreaccount1/{container}/{filename}", blobClient.Uri.ToString());
        }
    }
}
