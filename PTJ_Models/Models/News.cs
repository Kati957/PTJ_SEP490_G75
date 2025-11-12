using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class News
{
    [Key]
    [Column("NewsID")]
    public int NewsId { get; set; }

    [Column("AdminID")]
    public int AdminId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    [StringLength(255)]
    public string? ImageUrl { get; set; }

    [StringLength(50)]
    public string? Category { get; set; }

    public bool IsFeatured { get; set; }

    public int Priority { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime UpdatedAt { get; set; }

    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }

    [ForeignKey("AdminId")]
    [InverseProperty("News")]
    public virtual User Admin { get; set; } = null!;

    [InverseProperty("Entity")]
    public virtual ICollection<Image> Images { get; set; } = new List<Image>();
}
