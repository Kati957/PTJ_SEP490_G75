using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class PasswordResetToken
{
    [Key]
    [Column("TokenID")]
    public int TokenId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(200)]
    public string Token { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime Expiration { get; set; }

    public bool IsUsed { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("PasswordResetTokens")]
    public virtual User User { get; set; } = null!;
}
