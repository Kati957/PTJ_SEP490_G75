using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using PTJ_Models;
using PTJ_Models.Models;
using System;
using System.Threading.Tasks;

namespace PTJ_Service.ImageService
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly JobMatchingDbContext _context;

        public ImageService(Cloudinary cloudinary, JobMatchingDbContext context)
        {
            _cloudinary = cloudinary;
            _context = context;
        }
        public async Task<(string Url, string PublicId)> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File rỗng.");

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Upload thất bại.");

            return (uploadResult.SecureUrl.ToString(), uploadResult.PublicId);
        }

        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return;
            var delParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(delParams);
        }
    }
}
