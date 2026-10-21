using LearningPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Controllers;

[Authorize]
public class ResourcesController : Controller
{
    private readonly LearningPortalContext _db;

    public ResourcesController(LearningPortalContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var resources = await _db.LearningResources
            .Include(r => r.Category)
            .Include(r => r.Author)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return View(resources);
    }
}