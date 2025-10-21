using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class PasswordResetToken
{
    public int TokenId { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime Expiration { get; set; }

    public bool IsUsed { get; set; }

    public virtual User User { get; set; } = null!;
}
