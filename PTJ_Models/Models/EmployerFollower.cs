using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class EmployerFollower
{
    [Key]
    [Column("FollowID")]
    public int FollowId { get; set; }

    [Column("JobSeekerID")]
    public int JobSeekerId { get; set; }

    [Column("EmployerID")]
    public int EmployerId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime FollowDate { get; set; }

    public bool IsActive { get; set; }

    [ForeignKey("EmployerId")]
    [InverseProperty("EmployerFollowerEmployers")]
    public virtual User Employer { get; set; } = null!;

    [ForeignKey("JobSeekerId")]
    [InverseProperty("EmployerFollowerJobSeekers")]
    public virtual User JobSeeker { get; set; } = null!;
}
