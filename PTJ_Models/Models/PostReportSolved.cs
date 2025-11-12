using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("PostReport_Solved")]
[Index("PostReportId", Name = "UQ_PostReport_Solved_PostReportID", IsUnique = true)]
public partial class PostReportSolved
{
    [Key]
    [Column("SolvedPostReportID")]
    public int SolvedPostReportId { get; set; }

    [Column("PostReportID")]
    public int PostReportId { get; set; }

    [Column("AdminID")]
    public int AdminId { get; set; }

    [Column("AffectedUserID")]
    public int? AffectedUserId { get; set; }

    [Column("AffectedPostID")]
    public int? AffectedPostId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? AffectedPostType { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string ActionTaken { get; set; } = null!;

    public string? Reason { get; set; }

    [Column("NotificationID")]
    public int? NotificationId { get; set; }

    public bool AppliedAction { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SolvedAt { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("PostReportSolvedAdmins")]
    public virtual User Admin { get; set; } = null!;

    [ForeignKey("AffectedUserId")]
    [InverseProperty("PostReportSolvedAffectedUsers")]
    public virtual User? AffectedUser { get; set; }

    [ForeignKey("NotificationId")]
    [InverseProperty("PostReportSolveds")]
    public virtual Notification? Notification { get; set; }

    [ForeignKey("PostReportId")]
    [InverseProperty("PostReportSolved")]
    public virtual PostReport PostReport { get; set; } = null!;
}
