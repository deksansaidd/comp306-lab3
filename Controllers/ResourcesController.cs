using System.Security.Claims;
using LearningPortal.Models;
using LearningPortal.Services;
using LearningPortal.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Controllers;

[Authorize]
public class ResourcesController : Controller
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".png", ".jpg", ".jpeg" };
    private const long MaxFileBytes = 20 * 1024 * 1024;

    private readonly LearningPortalContext _db;
    private readonly IFileStorage _files;
    private readonly CommentService _comments;

    public ResourcesController(
        LearningPortalContext db,
        IFileStorage files,
        CommentService comments)
    {
        _db = db;
        _files = files;
        _comments = comments;
    }


    public ResourcesController(LearningPortalContext db, IFileStorage files)
    {
        _db = db;
        _files = files;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---------- LIST ----------

    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        string sort = "newest")
    {
        var query = _db.LearningResources
            .Include(r => r.Category)
            .Include(r => r.Author)
            .AsNoTracking()
            .AsQueryable();

        // Search by title or author's username
        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();

            query = query.Where(r =>
                r.Title.Contains(term) ||
                r.Author.Username.Contains(term));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(r =>
                r.CategoryId == categoryId.Value);
        }

        // Sort resources
        query = sort switch
        {
            "popular" => query
                .OrderByDescending(r => r.ViewCount)
                .ThenByDescending(r => r.CreatedDate),

            "oldest" => query
                .OrderBy(r => r.CreatedDate),

            _ => query
                .OrderByDescending(r => r.CreatedDate)
        };

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Sort = sort;

        ViewBag.Categories = await _db.Categories
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        var resources = await query.ToListAsync();

        return View(resources);
    }


    // ---------- DETAILS (counts one view) ----------
    public async Task<IActionResult> Details(int id, bool noCount = false)
    {
        if (!noCount)
        {
            // One atomic UPDATE in SQL, so two people viewing at once are both counted.
            await _db.LearningResources
                .Where(r => r.ResourceId == id)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ViewCount, r => r.ViewCount + 1));
        }

        var resource = await _db.LearningResources
            .Include(r => r.Category)
            .Include(r => r.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == id);

        if (resource == null) return NotFound();

        ViewBag.Comments = await _comments.GetCommentsAsync(id);

        return View(resource);
    }

    // ---------- OPEN / DOWNLOAD (file streams from S3 through the app) ----------
    public async Task<IActionResult> Download(int id, bool inline = false)
    {
        var resource = await _db.LearningResources.AsNoTracking().FirstOrDefaultAsync(r => r.ResourceId == id);
        if (resource == null) return NotFound();

        var stored = await _files.OpenReadAsync(resource.S3key);
        if (stored == null) return NotFound("The file for this resource was not found in S3.");

        if (inline) return File(stored.Stream, stored.ContentType);

        string safeTitle = string.Concat(resource.Title.Split(Path.GetInvalidFileNameChars()));
        return File(stored.Stream, stored.ContentType, safeTitle + Path.GetExtension(resource.S3key));
    }

    // ---------- CREATE ----------
    [Authorize(Roles = "Instructor")]
    [HttpGet]
    public async Task<IActionResult> Create() => View(await BuildForm(new ResourceFormViewModel()));

    [Authorize(Roles = "Instructor")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ResourceFormViewModel model)
    {
        if (model.File == null)
            ModelState.AddModelError(nameof(model.File), "Please choose a file to upload.");
        else
            ValidateFile(model.File);

        if (!ModelState.IsValid) return View(await BuildForm(model));

        string key = await _files.UploadAsync(model.File!);

        var resource = new LearningResource
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            CategoryId = model.CategoryId,
            AuthorId = CurrentUserId,
            CreatedDate = DateTime.UtcNow,
            S3key = key,
            ViewCount = 0
        };

        _db.LearningResources.Add(resource);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = resource.ResourceId });
    }

    // ---------- EDIT ----------
    [Authorize(Roles = "Instructor")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var resource = await _db.LearningResources.FindAsync(id);
        if (resource == null) return NotFound();
        if (resource.AuthorId != CurrentUserId) return Forbid();

        return View(await BuildForm(new ResourceFormViewModel
        {
            ResourceId = resource.ResourceId,
            Title = resource.Title,
            Description = resource.Description,
            CategoryId = resource.CategoryId,
            CurrentFileKey = resource.S3key
        }));
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ResourceFormViewModel model)
    {
        if (id != model.ResourceId) return BadRequest();

        var resource = await _db.LearningResources.FindAsync(id);
        if (resource == null) return NotFound();
        if (resource.AuthorId != CurrentUserId) return Forbid();

        if (model.File != null) ValidateFile(model.File);

        if (!ModelState.IsValid)
        {
            model.CurrentFileKey = resource.S3key;
            return View(await BuildForm(model));
        }

        resource.Title = model.Title.Trim();
        resource.Description = model.Description.Trim();
        resource.CategoryId = model.CategoryId;

        if (model.File != null)
        {
            // Replace the file: upload the new one, save, then remove the old one.
            string oldKey = resource.S3key;
            resource.S3key = await _files.UploadAsync(model.File);
            await _db.SaveChangesAsync();
            await _files.DeleteAsync(oldKey);
        }
        else
        {
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- DELETE ----------
    [Authorize(Roles = "Instructor")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var resource = await _db.LearningResources
            .Include(r => r.Category).Include(r => r.Author)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResourceId == id);

        if (resource == null) return NotFound();
        if (resource.AuthorId != CurrentUserId) return Forbid();
        return View(resource);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resource = await _db.LearningResources.FindAsync(id);
        if (resource == null) return RedirectToAction(nameof(Index));
        if (resource.AuthorId != CurrentUserId) return Forbid();

        string key = resource.S3key;
        _db.LearningResources.Remove(resource);
        await _db.SaveChangesAsync();
        await _files.DeleteAsync(key);          // remove the file from S3 as well

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------
    private void ValidateFile(IFormFile file)
    {
        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            ModelState.AddModelError("File", "Allowed file types: " + string.Join(", ", AllowedExtensions));
        if (file.Length > MaxFileBytes)
            ModelState.AddModelError("File", "The file must be 20 MB or smaller.");
    }

    private async Task<ResourceFormViewModel> BuildForm(ResourceFormViewModel model)
    {
        var categories = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync();
        model.Categories = categories
            .Select(c => new SelectListItem(c.CategoryName, c.CategoryId.ToString(), c.CategoryId == model.CategoryId))
            .ToList();
        return model;
    }
}