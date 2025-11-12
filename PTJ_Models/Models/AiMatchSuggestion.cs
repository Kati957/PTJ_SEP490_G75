using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("AI_MatchSuggestions")]
public partial class AiMatchSuggestion
{
    [Key]
    [Column("SuggestionID")]
    public int SuggestionId { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string SourceType { get; set; } = null!;

    [Column("SourceID")]
    public int SourceId { get; set; }

    [StringLength(30)]
    [Unicode(false)]
    public string TargetType { get; set; } = null!;

    [Column("TargetID")]
    public int TargetId { get; set; }

    public double RawScore { get; set; }

    public int MatchPercent { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? UpdatedAt { get; set; }
}
