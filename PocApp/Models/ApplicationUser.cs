using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PocApp.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    [StringLength(100)]
    public string? FullName { get; set; }

    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}
