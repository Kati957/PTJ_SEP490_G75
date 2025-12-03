using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerRegistrationRequest
{
    public int RequestId { get; set; }

    public string Email { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string CompanyName { get; set; } = null!;

    public string? CompanyDescription { get; set; }

    public string? ContactPerson { get; set; }

    public string ContactPhone { get; set; } = null!;

    public string? ContactEmail { get; set; }

    public string? Website { get; set; }

    public string? Address { get; set; }

    public string Status { get; set; } = null!;

    public string? AdminNote { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? UserId { get; set; }

    public virtual User? User { get; set; }
}
