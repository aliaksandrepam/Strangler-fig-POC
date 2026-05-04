using System.ComponentModel.DataAnnotations;

namespace Projects.Api.Contracts;

public record ProjectDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    string OwnerId,
    string? OwnerName,
    string? OwnerEmail,
    int TaskCount);

public class ProjectCreateDto
{
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public string OwnerId { get; set; } = string.Empty;
}

public class ProjectUpdateDto
{
    [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
    [Required] public string OwnerId { get; set; } = string.Empty;
}

public record OwnerDto(string Id, string? FullName, string? Email);

public record CalendarTaskDto(
    int Id,
    string Title,
    DateTime DueDate,
    string Status,           // "Todo" | "InProgress" | "Done" | "Blocked"
    int ProjectId,
    string? ProjectName);
