using TaskHub.Models.Enums;

namespace TaskHub.Models;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int ProjectId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public string? AssignedToUserId { get; set; }

    public bool IsDeleted { get; set; }

    public Project Project { get; set; } = null!;

    public ApplicationUser CreatedByUser { get; set; } = null!;

    public ApplicationUser? AssignedToUser { get; set; }

    public ICollection<TaskComment> Comments { get; set; } = [];
}
