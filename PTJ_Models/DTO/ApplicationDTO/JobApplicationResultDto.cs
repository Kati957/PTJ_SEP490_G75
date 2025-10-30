using System;

namespace PTJ_Models.DTO.ApplicationDTO
    {
   
    public class JobApplicationResultDto
        {
     
        public int CandidateListId { get; set; }
        public int JobSeekerId { get; set; }
        public string Username { get; set; } = string.Empty;

     
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public int? BirthYear { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Skills { get; set; }
        public string? Experience { get; set; }
        public string? Education { get; set; }
        public string? PreferredJobType { get; set; }
        public string? PreferredLocation { get; set; }

     
        public string Status { get; set; } = "Pending";
        public DateTime ApplicationDate { get; set; }
        public string? Notes { get; set; }

  
        public int EmployerPostId { get; set; }
        public string? PostTitle { get; set; }
        public string? CategoryName { get; set; }
        public string? EmployerName { get; set; }
        public string? Location { get; set; }
        public decimal? Salary { get; set; }
        public string? WorkHours { get; set; }
        public int? PhoneContact { get; set; }
        }
    }
