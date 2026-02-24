namespace EmpLeave.Services.SupabaseServices
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string fileUrl);
    }
}
