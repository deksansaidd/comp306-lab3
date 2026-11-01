using LearningPortal.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LearningPortal.ViewModels;

public class ResourceListViewModel
{
    public List<LearningResource> Resources { get; set; } = new();
    public string? Title { get; set; }
    public int? CategoryId { get; set; }
    public string? Author { get; set; }
    public string Sort { get; set; } = "newest";
    public List<SelectListItem> Categories { get; set; } = new();
}