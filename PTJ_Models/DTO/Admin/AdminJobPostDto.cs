using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PTJ_Models.DTO.Admin
{
    // Dto cho Employer Post (bên Employer đăng bài)
    public class AdminEmployerPostDto
        {
        public int EmployerPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string EmployerEmail { get; set; } = string.Empty;
        public string? EmployerName { get; set; }
        public string? CategoryName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        }

    public class AdminEmployerPostDetailDto : AdminEmployerPostDto
        {
        public string? Description { get; set; }
        public decimal? Salary { get; set; }
        public string? Requirements { get; set; }
        public string? WorkHours { get; set; }

        [JsonIgnore]
        public int ProvinceId { get; set; }

        [JsonIgnore]
        public int DistrictId { get; set; }

        [JsonIgnore]
        public int WardId { get; set; }

        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }

        public string? PhoneContact { get; set; }
        }


    // Dto cho JobSeeker Post (ứng viên đăng bài)
    public class AdminJobSeekerPostDto
        {
        public int JobSeekerPostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string JobSeekerEmail { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? CategoryName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        }

    public class AdminJobSeekerPostDetailDto : AdminJobSeekerPostDto
        {
        public string? Description { get; set; }

        [JsonIgnore]
        public int ProvinceId { get; set; }

        [JsonIgnore]
        public int DistrictId { get; set; }

        [JsonIgnore]
        public int WardId { get; set; }

        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }

        public string? PreferredWorkHours { get; set; }
        public string? Gender { get; set; }
        }


    }
