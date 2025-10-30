﻿using System;

namespace PTJ_Models.DTO.Admin
{
    
    public class UserDto
    {
        public int Id { get; set; }                    
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }             
        public bool IsVerified { get; set; }          
        public DateTime CreatedAt { get; set; }        
        public DateTime? LastLogin { get; set; }       
    }

  
    public class UserDetailDto : UserDto
    {
        public string? FullName { get; set; }          
        public string? PhoneNumber { get; set; }       
        public string? Address { get; set; }           
        public int? BirthYear { get; set; }           
        public string? Gender { get; set; }           
        public string? PreferredLocation { get; set; } 
    }
}
