namespace PTJ_Models.DTO.CategoryDTO
    {
    public class SubCategoryDTO
        {
        public class SubCategoryCreateDto
            {
            public string Name { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public string? Description { get; set; }
            public bool IsActive { get; set; } = true;
            }

        public class SubCategoryUpdateDto
            {
            public string? Name { get; set; }
            public int? CategoryId { get; set; }
            public string? Description { get; set; }
            public bool? IsActive { get; set; }
            }

        public class SubCategoryFilterDto
            {
            public string? Name { get; set; }
            public int? CategoryId { get; set; }
            }
        }
    }
