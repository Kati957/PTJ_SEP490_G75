using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerSubscription
{
    public int SubscriptionId { get; set; }

    public int UserId { get; set; }

    public int PlanId { get; set; }

    public int RemainingPosts { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual EmployerPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
