using Azure.Storage.Blobs;
using System;

namespace Gwa.Etl.Services
{
    public class AirWatchBlobClientService
    {
        private readonly string connectionString;
        private readonly string container;
        private readonly string filename;

        public AirWatchBlobClientService()
        {
            connectionString = Environment.GetEnvironmentVariable("GWA_ETL_STORAGE_CONNECTION_STRING", EnvironmentVariableTarget.Process);
            container = Environment.GetEnvironmentVariable("DATA_EXTRACT_CONTAINER", EnvironmentVariableTarget.Process);
            filename = Environment.GetEnvironmentVariable("DATA_EXTRACT_FILE_NAME", EnvironmentVariableTarget.Process);
        }

        public BlobClient CreateBlobClient()
        {
            BlobServiceClient serviceClient = new(connectionString);
            BlobContainerClient containerClient = serviceClient.GetBlobContainerClient(container);
            return containerClient.GetBlobClient(filename);
        }
    }
}
