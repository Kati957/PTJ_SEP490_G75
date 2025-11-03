using Microsoft.AspNetCore.Mvc;
using PTJ_Models.DTO.Image;
using PTJ_Service.ImageService;

namespace PTJ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IImageService _imageService;

        public ImageController(IImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload([FromForm] UploadImageRequest request)
        {
            var (url, publicId) = await _imageService.UploadImageAsync(request.File, request.Folder);
            return Ok(new { url, publicId });
        }

        [HttpDelete("delete/{publicId}")]
        public async Task<IActionResult> Delete(string publicId)
        {
            await _imageService.DeleteImageAsync(publicId);
            return Ok(new { message = "Xóa thành công" });
        }
    }
}
