using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    public string? CompanyName { get; set; }

    public string? CompanyDescription { get; set; }

    public string? CompanyLogo { get; set; }

    public string? Website { get; set; }

    public string? ContactName { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }

    public string? Address { get; set; }

    public decimal? AverageRating { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
