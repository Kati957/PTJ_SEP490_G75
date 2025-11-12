using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class PostReport
{
    [Key]
    [Column("PostReportID")]
    public int PostReportId { get; set; }

    [Column("ReporterID")]
    public int ReporterId { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string ReportType { get; set; } = null!;

    [Column("ReportedItemID")]
    public int ReportedItemId { get; set; }

    [Column("EmployerPostID")]
    public int? EmployerPostId { get; set; }

    [Column("JobSeekerPostID")]
    public int? JobSeekerPostId { get; set; }

    [Column("TargetUserID")]
    public int? TargetUserId { get; set; }

    public string? Reason { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("EmployerPostId")]
    [InverseProperty("PostReports")]
    public virtual EmployerPost? EmployerPost { get; set; }

    [ForeignKey("JobSeekerPostId")]
    [InverseProperty("PostReports")]
    public virtual JobSeekerPost? JobSeekerPost { get; set; }

    [InverseProperty("PostReport")]
    public virtual PostReportSolved? PostReportSolved { get; set; }

    [ForeignKey("ReporterId")]
    [InverseProperty("PostReportReporters")]
    public virtual User Reporter { get; set; } = null!;

    [ForeignKey("TargetUserId")]
    [InverseProperty("PostReportTargetUsers")]
    public virtual User? TargetUser { get; set; }
}
