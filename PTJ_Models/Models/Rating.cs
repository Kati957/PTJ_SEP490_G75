using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class Rating
{
    [Key]
    [Column("RatingID")]
    public int RatingId { get; set; }

    [Column("RaterID")]
    public int RaterId { get; set; }

    [Column("RateeID")]
    public int RateeId { get; set; }

    [Column("SubmissionID")]
    public int? SubmissionId { get; set; }

    [Column(TypeName = "decimal(3, 2)")]
    public decimal? RatingValue { get; set; }

    public string? Comment { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("RateeId")]
    [InverseProperty("RatingRatees")]
    public virtual User Ratee { get; set; } = null!;

    [ForeignKey("RaterId")]
    [InverseProperty("RatingRaters")]
    public virtual User Rater { get; set; } = null!;

    [ForeignKey("SubmissionId")]
    [InverseProperty("Ratings")]
    public virtual JobSeekerSubmission? Submission { get; set; }
}
