using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class Notification
{
    [Key]
    [Column("NotificationID")]
    public int NotificationId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string NotificationType { get; set; } = null!;

    [Column("RelatedItemID")]
    public int? RelatedItemId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public bool IsRead { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Notification")]
    public virtual ICollection<PostReportSolved> PostReportSolveds { get; set; } = new List<PostReportSolved>();

    [ForeignKey("UserId")]
    [InverseProperty("Notifications")]
    public virtual User User { get; set; } = null!;
}
