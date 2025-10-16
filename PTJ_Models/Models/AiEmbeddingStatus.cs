using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class AiEmbeddingStatus
{
    public int EmbeddingId { get; set; }

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string Model { get; set; } = null!;

    public int VectorDim { get; set; }

    public string PineconeId { get; set; } = null!;

    public string ContentHash { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? ErrorMsg { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
