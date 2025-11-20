
using System;
using System.ComponentModel.DataAnnotations;

namespace PTJ_Models.DTO.Admin
{
    public class AdminEmployerRegListItemDto
    {
        public int RequestId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string CompanyName { get; set; }
        public string ContactPhone { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class AdminEmployerRegDetailDto
    {
        public int RequestId { get; set; }

        public string Email { get; set; }
        public string Username { get; set; }

        public string CompanyName { get; set; }
        public string? CompanyDescription { get; set; }
        public string? ContactPerson { get; set; }
        public string ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }

        public string Status { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
    public class AdminEmployerRegRejectDto
    {
        [Required, MaxLength(2000)]
        public string Reason { get; set; }
    }
}
