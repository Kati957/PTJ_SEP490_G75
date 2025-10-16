using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class PostReportSolved
{
    public int SolvedPostReportId { get; set; }

    public int PostReportId { get; set; }

    public int AdminId { get; set; }

    public int AffectedUserId { get; set; }

    public string ActionTaken { get; set; } = null!;

    public string? Reason { get; set; }

    public int? NotificationId { get; set; }

    public DateTime? SolvedAt { get; set; }

    public virtual User Admin { get; set; } = null!;

    public virtual User AffectedUser { get; set; } = null!;

    public virtual Notification? Notification { get; set; }

    public virtual PostReport PostReport { get; set; } = null!;
}
