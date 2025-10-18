using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class SystemStatistic
{
    public int StatId { get; set; }

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

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
