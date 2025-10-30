using System;

namespace PTJ_Models.DTO.ApplicationDTO
    {
    
    public class JobApplicationCreateDto
        {
        public int JobSeekerId { get; set; }
        public int EmployerPostId { get; set; }
        public string? Note { get; set; }
        }
    }
