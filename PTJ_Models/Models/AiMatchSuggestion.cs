using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class AiMatchSuggestion
{
    public int SuggestionId { get; set; }

    public string SourceType { get; set; } = null!;

    public int SourceId { get; set; }

    public string TargetType { get; set; } = null!;

    public int TargetId { get; set; }

    public double RawScore { get; set; }

    public int MatchPercent { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
