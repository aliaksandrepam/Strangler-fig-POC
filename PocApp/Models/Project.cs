using System.ComponentModel.DataAnnotations;

namespace PocApp.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Owner")]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
