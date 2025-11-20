namespace PTJ_Models.DTO.CategoryDTO
    {
    public class CategoryDTO
        {
        public class CategoryReadDto
            {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            }

        public class CategoryCreateDto
            {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; } = true;
            }

        public class CategoryUpdateDto
            {
            public string? Name { get; set; }
            public string? Type { get; set; }
            public string? Description { get; set; }
            public bool? IsActive { get; set; }
            }

        public class CategoryFilterDto
            {
            public string? Name { get; set; }
            }
        }
    }
