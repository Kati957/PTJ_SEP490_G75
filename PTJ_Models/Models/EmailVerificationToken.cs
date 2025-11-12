using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Index("Token", Name = "UQ_EmailVerificationTokens_Token", IsUnique = true)]
public partial class EmailVerificationToken
{
    [Key]
    [Column("EVTokenID")]
    public int EvtokenId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(200)]
    [Unicode(false)]
    public string Token { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime ExpiresAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UsedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("EmailVerificationTokens")]
    public virtual User User { get; set; } = null!;
}
