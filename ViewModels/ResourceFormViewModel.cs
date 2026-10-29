using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LearningPortal.ViewModels;

public class ResourceFormViewModel
{
    public int ResourceId { get; set; }

    [Required, StringLength(150)]
    public string Title { get; set; } = "";

    [Required, StringLength(1000)]
    public string Description { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    public string? CurrentFileKey { get; set; }

    public List<SelectListItem> Categories { get; set; } = new();
}