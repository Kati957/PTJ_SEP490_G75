using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

[Table("Employer_CandidatesList")]
public partial class EmployerCandidatesList
{
    [Key]
    [Column("CandidateListID")]
    public int CandidateListId { get; set; }

    [Column("EmployerPostID")]
    public int EmployerPostId { get; set; }

    [Column("JobSeekerID")]
    public int JobSeekerId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ApplicationDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("EmployerPostId")]
    [InverseProperty("EmployerCandidatesLists")]
    public virtual EmployerPost EmployerPost { get; set; } = null!;

    [ForeignKey("JobSeekerId")]
    [InverseProperty("EmployerCandidatesLists")]
    public virtual User JobSeeker { get; set; } = null!;
}
