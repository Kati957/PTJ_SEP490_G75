using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class AiContentForEmbedding
{
    public int ContentId { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string? Lang { get; set; }

    public string CanonicalText { get; set; } = null!;

    public string Hash { get; set; } = null!;

    public DateTime? LastPreparedAt { get; set; }
}
