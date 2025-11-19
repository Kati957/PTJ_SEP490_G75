using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class SystemReport
{
    public int SystemReportId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? AdminNote { get; set; }

    public int? ProcessedByAdminId { get; set; }

    public virtual User? ProcessedByAdmin { get; set; }

    public virtual User User { get; set; } = null!;
}
