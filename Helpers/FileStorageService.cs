using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace LaundryDashAPI_2.Helpers
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string basePath;

        // Constructor to set the base path for file storage
        public FileStorageService()
        {
            // Assuming files are stored in a folder named "Uploads" within the project directory
            basePath = Path.Combine(Directory.GetCurrentDirectory(), "LaundryShopImages");
        }

        // Delete the file from storage
        public async Task DeleteFile(string fileRoute, string containerName)
        {
            string filePath = Path.Combine(basePath, fileRoute);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            await Task.CompletedTask;
        }

        // Save the file to storage
        public async Task<string> SaveFile(string containerName, IFormFile file)
        {
            // Ensure the container directory exists
            string containerPath = Path.Combine(basePath, containerName);
            Directory.CreateDirectory(containerPath);

            // Generate a unique file name
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            // Full file path
            string filePath = Path.Combine(containerPath, fileName);

            // Save the file to the server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the relative path to store in the database (or a URL)
            return Path.Combine(containerName, fileName);
        }

        // Edit the file in storage
        public async Task<string> EditFile(string containerName, IFormFile file, string fileRoute)
        {
            // Delete the existing file before uploading the new one
            await DeleteFile(fileRoute, containerName);

            // Save the new file
            return await SaveFile(containerName, file);
        }
    }
}
