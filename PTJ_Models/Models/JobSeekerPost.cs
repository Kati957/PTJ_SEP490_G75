using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class JobSeekerPost
{
    [Key]
    [Column("JobSeekerPostID")]
    public int JobSeekerPostId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? Age { get; set; }

    [StringLength(10)]
    public string? Gender { get; set; }

    [StringLength(50)]
    public string? PreferredWorkHours { get; set; }

    [StringLength(255)]
    public string? PreferredLocation { get; set; }

    [Column("CategoryID")]
    public int? CategoryId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? PhoneContact { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [ForeignKey("CategoryId")]
    [InverseProperty("JobSeekerPosts")]
    public virtual Category? Category { get; set; }

    [InverseProperty("JobSeekerPost")]
    public virtual ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();

    [ForeignKey("UserId")]
    [InverseProperty("JobSeekerPosts")]
    public virtual User User { get; set; } = null!;
}
