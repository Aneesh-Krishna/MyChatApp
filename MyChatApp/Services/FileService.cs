using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
namespace MyChatApp.Services
{
    public class FileService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public FileService(IConfiguration configuration)
        {
            var blobServiceClient = new BlobServiceClient(configuration["AzureBlobStorage:ConnectionString"]);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(configuration["AzureBlobStorage:mychatapp"]);
            _blobContainerClient.CreateIfNotExists();
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            try
            {
                string blobName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var blobClient = _blobContainerClient.GetBlobClient(blobName);

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream);
                }

                return blobClient.Uri.ToString();
            }

            catch (Exception ex)
            {
                throw new Exception("File upload failed.", ex);
            }
        }

        public async Task<bool> DeleteFileAync(string fileUrl)
        {
            try
            {
                string blobName = new Uri(fileUrl).Segments[^1];
                var blobClient = _blobContainerClient.GetBlobClient(blobName);
                return await blobClient.DeleteIfExistsAsync();
            }

            catch(Exception ex)
            {
                throw new Exception("File deletion failed.", ex);
            }
        }
    }
}
