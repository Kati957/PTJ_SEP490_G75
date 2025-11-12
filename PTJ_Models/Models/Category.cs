using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class Category
{
    [Key]
    [Column("CategoryID")]
    public int CategoryId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(20)]
    public string Type { get; set; } = null!;

    [StringLength(255)]
    public string? Description { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<EmployerPost> EmployerPosts { get; set; } = new List<EmployerPost>();

    [InverseProperty("Category")]
    public virtual ICollection<JobSeekerPost> JobSeekerPosts { get; set; } = new List<JobSeekerPost>();
}
