using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerInvitation
{
    public int InvitationId { get; set; }

    public int EmployerId { get; set; }

    public int JobSeekerId { get; set; }

    public int? EmployerPostId { get; set; }

    public string? Message { get; set; }

    public string InvitationType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual User Employer { get; set; } = null!;

    public virtual EmployerPost? EmployerPost { get; set; }

    public virtual User JobSeeker { get; set; } = null!;
}
