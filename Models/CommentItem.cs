
namespace LearningPortal.Models;

public class CommentItem
{
    public int ResourceId { get; set; }

    public string CommentId { get; set; } = "";

    public int UserId { get; set; }

    public string Username { get; set; } = "";

    public string Text { get; set; } = "";

    public DateTime CreatedUtc { get; set; }

    public bool CanEdit(int currentUserId) =>
        UserId == currentUserId &&
        DateTime.UtcNow >= CreatedUtc &&
        DateTime.UtcNow - CreatedUtc <= TimeSpan.FromHours(24);
}
