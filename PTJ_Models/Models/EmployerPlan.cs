using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerPlan
{
    public int PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal Price { get; set; }

    public int MaxPosts { get; set; }

    public int? DurationDays { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<EmployerSubscription> EmployerSubscriptions { get; set; } = new List<EmployerSubscription>();

    public virtual ICollection<EmployerTransaction> EmployerTransactions { get; set; } = new List<EmployerTransaction>();
}
