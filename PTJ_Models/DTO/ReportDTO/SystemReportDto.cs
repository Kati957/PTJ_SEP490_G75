using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO
{
    public class SystemReportCreateDto
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }
    }

    public class SystemReportViewDto
    {
        public int ReportId { get; set; }
        public string UserEmail { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class SystemReportUpdateDto
    {
        [Required]
        public string Status { get; set; }
    }
}
