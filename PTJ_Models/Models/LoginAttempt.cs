using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class LoginAttempt
{
    public int AttemptId { get; set; }

    public int? UserId { get; set; }

    public string? UsernameOrEmail { get; set; }

    public string? Ipaddress { get; set; }

    public string? DeviceInfo { get; set; }

    public bool IsSuccess { get; set; }

    public string? Message { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual User? User { get; set; }
}
