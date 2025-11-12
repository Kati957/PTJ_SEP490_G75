using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("AI_ContentForEmbedding")]
public partial class AiContentForEmbedding
{
    [Key]
    [Column("ContentID")]
    public int ContentId { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string EntityType { get; set; } = null!;

    [Column("EntityID")]
    public int EntityId { get; set; }

    [StringLength(10)]
    [Unicode(false)]
    public string? Lang { get; set; }

    public string CanonicalText { get; set; } = null!;

    [StringLength(64)]
    [Unicode(false)]
    public string Hash { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime LastPreparedAt { get; set; }
}
