using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class AiQueryCache
{
    public int CacheId { get; set; }

    public string Namespace { get; set; } = null!;

    public int EntityId { get; set; }

    public string JsonResults { get; set; } = null!;

    public DateTime CachedAt { get; set; }
}
