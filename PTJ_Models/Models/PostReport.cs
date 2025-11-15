using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class PostReport
{
    public int PostReportId { get; set; }

    public int ReporterId { get; set; }

    public string ReportType { get; set; } = null!;

    public int ReportedItemId { get; set; }

    public int? EmployerPostId { get; set; }

    public int? JobSeekerPostId { get; set; }

    public int? TargetUserId { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual EmployerPost? EmployerPost { get; set; }

    public virtual JobSeekerPost? JobSeekerPost { get; set; }

    public virtual PostReportSolved? PostReportSolved { get; set; }

    public virtual User Reporter { get; set; } = null!;

    public virtual User? TargetUser { get; set; }
}
