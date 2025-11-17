using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PTJ_Models.DTO.Admin
{
    public class AdminNewsDto
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = null!;
        public string? Category { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AdminNewsDetailDto
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public bool IsFeatured { get; set; }
        public int Priority { get; set; }
        public bool IsPublished { get; set; }
        public int AdminId { get; set; }
        public string? AdminEmail { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AdminCreateNewsDto
    {
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public string? Category { get; set; }
        public bool IsFeatured { get; set; }
        public int Priority { get; set; }
        public bool IsPublished { get; set; } = false;
        public IFormFile? CoverImage { get; set; }
    }

    public class AdminUpdateNewsDto
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? Category { get; set; }
        public bool IsFeatured { get; set; }
        public int Priority { get; set; }
        public bool? IsPublished { get; set; }  
        public IFormFile? CoverImage { get; set; }
    }
}
