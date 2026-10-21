using System;
using System.Collections.Generic;

namespace LearningPortal.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public virtual ICollection<LearningResource> LearningResources { get; set; } = new List<LearningResource>();
}
