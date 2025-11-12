using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class Image
{
    [Key]
    [Column("ImageID")]
    public int ImageId { get; set; }

    [StringLength(50)]
    public string EntityType { get; set; } = null!;

    [Column("EntityID")]
    public int EntityId { get; set; }

    [StringLength(500)]
    public string Url { get; set; } = null!;

    [StringLength(255)]
    public string PublicId { get; set; } = null!;

    [StringLength(20)]
    public string? Format { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("EntityId")]
    [InverseProperty("Images")]
    public virtual News Entity { get; set; } = null!;
}
