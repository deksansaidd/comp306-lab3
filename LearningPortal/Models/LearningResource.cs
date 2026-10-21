using System;
using System.Collections.Generic;

namespace LearningPortal.Models;

public partial class LearningResource
{
    public int ResourceId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int CategoryId { get; set; }

    public int AuthorId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string S3key { get; set; } = null!;

    public int ViewCount { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual Category Category { get; set; } = null!;
}
