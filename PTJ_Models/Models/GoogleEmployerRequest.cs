using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class GoogleEmployerRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? PictureUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? AdminNote { get; set; }

    public virtual User User { get; set; } = null!;
}
