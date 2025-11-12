using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace PTJ_Models.Models;

public partial class LocationCache
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string Address { get; set; } = null!;

    public double Lat { get; set; }

    public double Lng { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastUpdated { get; set; }
}
