using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class UserActivityLog
{
    public int LogId { get; set; }

    public int UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public string? Details { get; set; }

    public string? Ipaddress { get; set; }

    public string? DeviceInfo { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual User User { get; set; } = null!;
}
