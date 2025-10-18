using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerFollower
{
    public int FollowId { get; set; }

    public int JobSeekerId { get; set; }

    public int EmployerId { get; set; }

    public DateTime FollowDate { get; set; }

    public bool IsActive { get; set; }

    public virtual User Employer { get; set; } = null!;

    public virtual User JobSeeker { get; set; } = null!;
}
