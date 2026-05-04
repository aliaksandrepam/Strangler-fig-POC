using System.ComponentModel.DataAnnotations;

namespace Projects.Api.Models;

public class Project
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public OwnerView? Owner { get; set; }
}

// Read-only projection over the monolith's AspNetUsers table.
// We only know about the columns we need for displaying owners.
public class OwnerView
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FullName { get; set; }
}
