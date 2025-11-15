using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class ExternalLogin
{
    public int ExternalLoginId { get; set; }

    public int UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderKey { get; set; } = null!;

    public string? Email { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
