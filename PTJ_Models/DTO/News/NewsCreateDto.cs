using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO.News
{
    public class NewsCreateDto
    {
        public int AdminID { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Category { get; set; }
        public bool IsFeatured { get; set; }
        public int Priority { get; set; }

        public IFormFile? CoverImage { get; set; }
        public List<IFormFile>? GalleryImages { get; set; }
    }
}
