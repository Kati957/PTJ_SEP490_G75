using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class NotificationTemplate
{
    public int TemplateId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string TitleTemplate { get; set; } = null!;

    public string MessageTemplate { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
