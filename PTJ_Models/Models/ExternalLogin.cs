using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Index("Provider", "ProviderKey", Name = "UQ_ExternalLogins_ProviderKey", IsUnique = true)]
public partial class ExternalLogin
{
    [Key]
    [Column("ExternalLoginID")]
    public int ExternalLoginId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string Provider { get; set; } = null!;

    [StringLength(200)]
    public string ProviderKey { get; set; } = null!;

    [StringLength(100)]
    public string? Email { get; set; }

    public bool EmailVerified { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("ExternalLogins")]
    public virtual User User { get; set; } = null!;
}
