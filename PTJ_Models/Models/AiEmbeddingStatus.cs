using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("AI_EmbeddingStatus")]
public partial class AiEmbeddingStatus
{
    [Key]
    [Column("EmbeddingID")]
    public int EmbeddingId { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string EntityType { get; set; } = null!;

    [Column("EntityID")]
    public int EntityId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Model { get; set; } = null!;

    public int VectorDim { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string PineconeId { get; set; } = null!;

    [StringLength(64)]
    [Unicode(false)]
    public string ContentHash { get; set; } = null!;

    [StringLength(20)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [StringLength(1000)]
    public string? ErrorMsg { get; set; }

    public string? VectorData { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }
}
