using Azure.Storage.Blobs;

namespace GameStore.Services.Impl
{
    public class BlobService
    {
        private readonly string _connectionString;

        public BlobService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string containerName)
        {
            try
            {
                var blobContainerClient = new BlobContainerClient(_connectionString, containerName);

                // Ensure the container exists
                await blobContainerClient.CreateIfNotExistsAsync();

                var blobClient = blobContainerClient.GetBlobClient(fileName);

                await blobClient.UploadAsync(fileStream, overwrite: true);

                // Return the URL of the uploaded file
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while uploading the file.", ex);
            }
        }
    }
}
