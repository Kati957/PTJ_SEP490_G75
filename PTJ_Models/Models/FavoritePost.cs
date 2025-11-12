using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class FavoritePost
{
    [Key]
    [Column("FavoriteID")]
    public int FavoriteId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(20)]
    public string? PostType { get; set; }

    [Column("PostID")]
    public int PostId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    public string? Notes { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("FavoritePosts")]
    public virtual User User { get; set; } = null!;
}
