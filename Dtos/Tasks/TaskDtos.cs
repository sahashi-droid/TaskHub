using System.ComponentModel.DataAnnotations;
using TaskHub.Dtos.Auth;
using TaskHub.Dtos.Comments;
using TaskHub.Models.Enums;

namespace TaskHub.Dtos.Tasks;

public class CreateTaskRequestDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    public DateTime? DueDateUtc { get; set; }

    [Required]
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public string? AssignedToUserId { get; set; }
}

public class UpdateTaskRequestDto : CreateTaskRequestDto
{
}

public class TaskSummaryDto
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string Title { get; set; } = string.Empty;

    public TaskItemStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public UserSummaryDto? AssignedTo { get; set; }

    public UserSummaryDto CreatedBy { get; set; } = new();
}

public class TaskDetailsDto : TaskSummaryDto
{
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public IReadOnlyList<CommentDto> Comments { get; set; } = [];
}
