using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO.Image
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; } = null!;
        public string Folder { get; set; } = "News";
    }
}
