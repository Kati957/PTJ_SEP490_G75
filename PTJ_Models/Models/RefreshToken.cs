using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Index("Token", Name = "UQ_RefreshTokens_Token", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    [Column("RefreshTokenID")]
    public int RefreshTokenId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(200)]
    public string Token { get; set; } = null!;

    [StringLength(100)]
    public string JwtId { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime IssuedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ExpiresAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? RevokedAt { get; set; }

    [StringLength(255)]
    public string? DeviceInfo { get; set; }

    [Column("IPAddress")]
    [StringLength(50)]
    [Unicode(false)]
    public string? Ipaddress { get; set; }

    public bool IsRevoked { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("RefreshTokens")]
    public virtual User User { get; set; } = null!;
}
