using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class Image
{
    public int ImageId { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string Url { get; set; } = null!;

    public string PublicId { get; set; } = null!;

    public string? Format { get; set; }

    public DateTime CreatedAt { get; set; }
}
