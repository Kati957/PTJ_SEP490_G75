using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.News
{
    public class NewsReadDto
    {
        public int NewsID { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public List<string>? GalleryUrls { get; set; }
        public string? Category { get; set; }
        public bool IsFeatured { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
