using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace DataManagementApi.Services
{
    public interface IFileService
    {
        Task<(string fileName, string filePath, string fileType, long fileSize)> SaveFileAsync(IFormFile file, string subDirectory);
        void DeleteFile(string filePath);
        string GetFileUrl(string fileName, string subDirectory);
        string GetRootPath();
    }

    public class FileService : IFileService
    {
        private readonly string _rootPath;
        private readonly string _baseUrl;

        public FileService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _rootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
            _baseUrl = configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
        }

        public async Task<(string fileName, string filePath, string fileType, long fileSize)> SaveFileAsync(IFormFile file, string subDirectory)
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty");
            }

            // Validate file size (20MB max)
            if (file.Length > 20 * 1024 * 1024)
            {
                throw new ArgumentException("File size exceeds the limit of 20MB");
            }

            // Validate file type (only PDF and DOCX)
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf" && fileExtension != ".docx")
            {
                throw new ArgumentException("Only PDF and DOCX files are allowed");
            }

            // Create directory if it doesn't exist
            var directoryPath = Path.Combine(_rootPath, subDirectory);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Generate unique file name
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(directoryPath, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (uniqueFileName, filePath, file.ContentType, file.Length);
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public string GetFileUrl(string fileName, string subDirectory)
        {
            return $"{_baseUrl}/{subDirectory}/{fileName}";
        }
        
        public string GetRootPath()
        {
            return _rootPath;
        }
    }
}