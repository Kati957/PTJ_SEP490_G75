using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class JobSeekerApplicationList
{
    public int ApplicationListId { get; set; }

    public int JobSeekerId { get; set; }

    public int EmployerPostId { get; set; }

    public DateTime? ApplicationDate { get; set; }

    public string? Status { get; set; }

    public string? Notes { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual EmployerPost EmployerPost { get; set; } = null!;

    public virtual User JobSeeker { get; set; } = null!;
}
