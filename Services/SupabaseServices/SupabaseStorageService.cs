using Supabase;

namespace EmpLeave.Services.SupabaseServices
{
    // Ensure SupabaseStorageService implements IFileStorageService
    public class SupabaseStorageService : IFileStorageService
    {
        private readonly IConfiguration _config;
        private readonly Client _client;
        private readonly string _bucket;

        public SupabaseStorageService(IConfiguration config, Client client, string bucket)
        {
            _config = config;
            _client = client;
            _bucket = bucket;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"{folder}/{Guid.NewGuid()}{fileExtension}";

            using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            await _client.Storage
                .From(_bucket)
                .Upload(fileBytes, fileName);

            var publicUrl = _client.Storage
                .From(_bucket)
                .GetPublicUrl(fileName);

            return publicUrl;
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            var filePath = uri.AbsolutePath.Split($"/object/public/{_bucket}/").LastOrDefault();

            if (string.IsNullOrEmpty(filePath))
                return false;

            await _client.Storage
                .From(_bucket)
                .Remove(new List<string> { filePath });

            return true;
        }
    }
}
