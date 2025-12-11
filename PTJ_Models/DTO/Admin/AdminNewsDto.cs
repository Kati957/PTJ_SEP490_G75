using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Tiêu đề phải từ 5 đến 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        [StringLength(10000, MinimumLength = 20, ErrorMessage = "Nội dung phải có ít nhất 20 ký tự.")]
        public string Content { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Danh mục tối đa 100 ký tự.")]
        public string? Category { get; set; }

        public bool IsFeatured { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Priority phải >= 0.")]
        public int Priority { get; set; }

        public bool IsPublished { get; set; } = false;

        // Optional: Validate size & type
        [DataType(DataType.Upload)]
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
