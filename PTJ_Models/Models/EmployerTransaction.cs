using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class EmployerTransaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public int PlanId { get; set; }

    public string? PayOsorderCode { get; set; }

    public decimal? Amount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? RawWebhookData { get; set; }

    public string? QrCodeUrl { get; set; }

    public DateTime? QrExpiredAt { get; set; }

    public bool EmailSent { get; set; }

    public virtual EmployerPlan Plan { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
