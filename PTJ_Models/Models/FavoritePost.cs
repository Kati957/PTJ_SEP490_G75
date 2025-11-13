using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class FavoritePost
{
    public int FavoriteId { get; set; }

    public int UserId { get; set; }

    public string? PostType { get; set; }

    public int PostId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Notes { get; set; }

    public virtual User User { get; set; } = null!;
}
