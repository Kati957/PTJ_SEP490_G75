using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class News
{
    public int NewsId { get; set; }

    public int AdminId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string? Category { get; set; }

    public bool IsFeatured { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }

    public virtual User Admin { get; set; } = null!;
}
