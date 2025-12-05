using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class GoogleEmployerRegListDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PictureUrl { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public class GoogleEmployerRegDetailDto
        {
            public int Id { get; set; }
            public string DisplayName { get; set; } = null!;
            public string Email { get; set; } = null!;
            public string? PictureUrl { get; set; }
            public string Status { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
            public string? AdminNote { get; set; }
        }

    }
}