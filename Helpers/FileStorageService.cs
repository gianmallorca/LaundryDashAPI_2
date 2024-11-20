using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LaundryDashAPI_2.Helpers
{
    public class FileStorageService : IFileStorageService
    {

        private readonly string basePath;

        public FileStorageService(IConfiguration configuration)
        {
            // Ensure the base path for local storage is initialized
            basePath = configuration["LocalStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "LaundryShopImages");
        }

        // Method to delete a file, return type is Task (async operation with no result)
        public async Task DeleteFile(string fileRoute, string containerName)
        {
            if (string.IsNullOrEmpty(fileRoute))
            {
                return;
            }

            var filePath = Path.Combine(basePath, containerName, Path.GetFileName(fileRoute));

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath)); // Ensure delete runs asynchronously
            }
        }

        // Method to edit a file, return type is Task<string> (async operation with a string result)
        public async Task<string> EditFile(string containerName, IFormFile file, string fileRoute)
        {
            // Delete the old file if it exists
            await DeleteFile(fileRoute, containerName);

            // Save the new file and return the file path
            return await SaveFile(containerName, file);
        }

        // Method to save a file, return type is Task<string> (async operation with a string result)
        public async Task<string> SaveFile(string containerName, IFormFile file)
        {
            // Ensure the container directory exists
            var containerPath = Path.Combine(basePath, containerName);
            Directory.CreateDirectory(containerPath);

            // Generate a unique file name
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            // Full file path
            var filePath = Path.Combine(containerPath, fileName);

            // Save the file to the local storage
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative file path for storage in the database
            return Path.Combine(containerName, fileName).Replace("\\", "/"); // Normalize path for URL usage
        }
    }
}

