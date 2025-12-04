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
}
