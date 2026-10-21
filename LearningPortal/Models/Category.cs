using System;
using System.Collections.Generic;

namespace LearningPortal.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<LearningResource> LearningResources { get; set; } = new List<LearningResource>();
}
