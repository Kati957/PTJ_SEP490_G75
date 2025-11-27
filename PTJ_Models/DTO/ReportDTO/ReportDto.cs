using System;

namespace PTJ_Models.DTO
{

    // REQUEST DTO – FE dùng để gửi báo cáo bài đăng

    public class CreatePostReportDto
    {
        public int PostId { get; set; }            
        public string PostType { get; set; } = "";  
        public string? Reason { get; set; }
    }



    // RESPONSE DTO – dùng cho trang "Báo cáo của tôi"

    public class MyReportDto
    {
        public int ReportId { get; set; }
        public string ReportType { get; set; } = string.Empty;

        public int? PostId { get; set; }            
        public string? PostType { get; set; }       
        public string? PostTitle { get; set; }      

        public string Status { get; set; } = "Pending";
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
