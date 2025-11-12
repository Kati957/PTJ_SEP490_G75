using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminNewsDto
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Status { get; set; }    // Active / Hidden
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminNewsDetailDto
    {
        public int NewsId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int AdminId { get; set; }
        public string? AdminEmail { get; set; }
    }

    public class AdminCreateNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
    }

    public class AdminUpdateNewsDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
    }
}
