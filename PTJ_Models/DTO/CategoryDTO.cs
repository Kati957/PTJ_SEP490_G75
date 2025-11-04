namespace PTJ_Models.DTO.CategoryDTO
    {
    public class CategoryDTO
        {
        // ✅ DTO cho việc đọc dữ liệu (Response)
        public class Read
            {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            }

        // ✅ DTO cho việc tạo mới (Create)
        public class Create
            {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; } = true;
            }

        // ✅ DTO cho việc cập nhật (Update)
        public class Update
            {
            public string? Name { get; set; }
            public string? Type { get; set; }
            public string? Description { get; set; }
            public bool? IsActive { get; set; }
            }
        }
    }
