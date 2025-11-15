using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class LocationCache
{
    public int Id { get; set; }

    public string Address { get; set; } = null!;

    public double Lat { get; set; }

    public double Lng { get; set; }

    public DateTime? LastUpdated { get; set; }
}
