using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    public class AdminCategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryGroup { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; } 
    }

    public class AdminCreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryGroup { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AdminUpdateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? CategoryGroup { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
