
using System.Security.Claims;
using LearningPortal.Models;
using LearningPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Controllers;

[Authorize]
public class CommentsController : Controller
{
    private readonly CommentService _comments;
    private readonly LearningPortalContext _db;

    public CommentsController(
        CommentService comments,
        LearningPortalContext db)
    {
        _comments = comments;
        _db = db;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // POST: /Comments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int resourceId,
        string text)
    {
        if (!await _db.LearningResources
            .AnyAsync(r => r.ResourceId == resourceId))
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(text) ||
            text.Trim().Length > 1000)
        {
            TempData["CommentError"] =
                "Comments must contain between 1 and 1000 characters.";

            return RedirectToAction(
                "Details",
                "Resources",
                new { id = resourceId, noCount = true });
        }

        var comment = new CommentItem
        {
            ResourceId = resourceId,
            UserId = CurrentUserId,
            Username = User.Identity?.Name ?? "User",
            Text = text.Trim()
        };

        await _comments.AddCommentAsync(comment);

        return RedirectToAction(
            "Details",
            "Resources",
            new { id = resourceId, noCount = true });
    }

    // POST: /Comments/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int resourceId,
        string commentId,
        string text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            text.Trim().Length > 1000)
        {
            TempData["CommentError"] =
                "Comments must contain between 1 and 1000 characters.";
        }
        else
        {
            bool updated = await _comments.EditCommentAsync(
                resourceId,
                commentId,
                CurrentUserId,
                text);

            if (!updated)
            {
                TempData["CommentError"] =
                    "You can only edit your own comments within 24 hours.";
            }
        }

        return RedirectToAction(
            "Details",
            "Resources",
            new { id = resourceId, noCount = true });
    }
}
