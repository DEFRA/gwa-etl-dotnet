using Azure.Storage.Blobs;

namespace Gwa.Etl.Services
{
    public class AirWatchBlobClientService
    {
        private readonly string connectionString;
        private readonly string container;
        private readonly string filename;

        public AirWatchBlobClientService(string connectionString, string container, string filename)
        {
            this.connectionString = connectionString;
            this.container = container;
            this.filename = filename;
        }

        public BlobClient CreateBlobClient()
        {
            BlobServiceClient serviceClient = new(connectionString);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container);
            return containerClient.GetBlobClient(filename);
        }
    }
}
