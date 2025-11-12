using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class LoginAttempt
{
    [Key]
    [Column("AttemptID")]
    public int AttemptId { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    [StringLength(100)]
    public string? UsernameOrEmail { get; set; }

    [Column("IPAddress")]
    [StringLength(50)]
    public string? Ipaddress { get; set; }

    [StringLength(255)]
    public string? DeviceInfo { get; set; }

    public bool IsSuccess { get; set; }

    [StringLength(255)]
    public string? Message { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Timestamp { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("LoginAttempts")]
    public virtual User? User { get; set; }
}
