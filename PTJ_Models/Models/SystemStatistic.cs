using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class SystemStatistic
{
    [Key]
    [Column("StatID")]
    public int StatId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime StatDate { get; set; }

    public int TotalUsers { get; set; }

    public int TotalJobSeekers { get; set; }

    public int TotalEmployers { get; set; }

    public int TotalPosts { get; set; }

    public int TotalApplications { get; set; }

    public int TotalReports { get; set; }

    public int TotalNews { get; set; }

    public int TotalLogins { get; set; }

    public int TotalActiveUsers { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }
}
