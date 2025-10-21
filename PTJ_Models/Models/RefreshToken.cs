using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public string JwtId { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? DeviceInfo { get; set; }

    public string? Ipaddress { get; set; }

    public bool IsRevoked { get; set; }

    public virtual User User { get; set; } = null!;
}
