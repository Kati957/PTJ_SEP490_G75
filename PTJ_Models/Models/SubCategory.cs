using System;
using System.Collections.Generic;

namespace PTJ_Models.Models;

public partial class SubCategory
{
    public int SubCategoryId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Keywords { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<EmployerPost> EmployerPosts { get; set; } = new List<EmployerPost>();

    public virtual ICollection<JobSeekerPost> JobSeekerPosts { get; set; } = new List<JobSeekerPost>();
}
