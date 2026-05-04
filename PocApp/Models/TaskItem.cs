using System.ComponentModel.DataAnnotations;

namespace PocApp.Models;

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
    Blocked = 3
}

public class TaskItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Due date")]
    public DateTime DueDate { get; set; } = DateTime.Today;

    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    [Required]
    [Display(Name = "Project")]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    [Display(Name = "Assigned to")]
    public string? AssignedUserId { get; set; }
    public ApplicationUser? AssignedUser { get; set; }
}
