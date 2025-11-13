using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string NotificationType { get; set; } = null!;

    public int? RelatedItemId { get; set; }

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PostReportSolved> PostReportSolveds { get; set; } = new List<PostReportSolved>();

    public virtual User User { get; set; } = null!;
}
