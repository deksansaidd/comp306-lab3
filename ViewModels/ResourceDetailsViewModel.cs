using LearningPortal.Models;

namespace LearningPortal.ViewModels;

public class ResourceDetailsViewModel
{
    public LearningResource Resource { get; set; } = null!;
    public List<CommentItem> Comments { get; set; } = new();
    public int CurrentUserId { get; set; }
}