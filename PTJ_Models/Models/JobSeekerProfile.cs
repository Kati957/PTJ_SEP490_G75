using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Index("UserId", Name = "UQ_JobSeekerProfiles_UserID", IsUnique = true)]
public partial class JobSeekerProfile
{
    [Key]
    [Column("ProfileID")]
    public int ProfileId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    public int? BirthYear { get; set; }

    [StringLength(255)]
    public string? ProfilePicture { get; set; }

    [StringLength(255)]
    public string? PreferredLocation { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [StringLength(255)]
    public string? ProfilePicturePublicId { get; set; }

    public bool IsPictureHidden { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? ContactPhone { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("JobSeekerProfile")]
    public virtual User User { get; set; } = null!;
}
