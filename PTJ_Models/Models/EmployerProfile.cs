using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Index("UserId", Name = "UQ_EmployerProfiles_UserID", IsUnique = true)]
public partial class EmployerProfile
{
    [Key]
    [Column("ProfileID")]
    public int ProfileId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    [StringLength(100)]
    public string? ContactName { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? ContactPhone { get; set; }

    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    [StringLength(255)]
    public string? Website { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [StringLength(255)]
    public string? AvatarPublicId { get; set; }

    public bool IsAvatarHidden { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("EmployerProfile")]
    public virtual User User { get; set; } = null!;
}
