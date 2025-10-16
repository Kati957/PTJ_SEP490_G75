using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class Rating
{
    public int RatingId { get; set; }

    public int RaterId { get; set; }

    public int RateeId { get; set; }

    public int? ApplicationId { get; set; }

    public decimal RatingValue { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Application? Application { get; set; }

    public virtual User Ratee { get; set; } = null!;

    public virtual User Rater { get; set; } = null!;
}
