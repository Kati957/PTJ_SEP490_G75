using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<EmployerPost> EmployerPosts { get; set; } = new List<EmployerPost>();

    public virtual ICollection<JobSeekerPost> JobSeekerPosts { get; set; } = new List<JobSeekerPost>();
}
