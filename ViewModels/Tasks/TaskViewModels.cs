using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskHub.Models.Enums;

namespace TaskHub.ViewModels.Tasks;

public class TaskIndexViewModel
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public TaskItemStatus? SelectedStatus { get; set; }

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    public IReadOnlyList<TaskListItemViewModel> Tasks { get; set; } = [];
}

public class TaskListItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public TaskItemStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public string? AssigneeName { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public DateTime? DueDateUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class TaskFormViewModel
{
    public int? Id { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public bool ProjectIsArchived { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Due date (UTC)")]
    public DateTime? DueDateUtc { get; set; }

    [Required]
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    [Required]
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    [Display(Name = "Assigned to")]
    public string? AssignedToUserId { get; set; }

    public IReadOnlyList<SelectListItem> AssigneeOptions { get; set; } = [];

    public IReadOnlyList<SelectListItem> StatusOptions { get; set; } = [];

    public IReadOnlyList<SelectListItem> PriorityOptions { get; set; } = [];
}

public class AddTaskCommentViewModel
{
    public int TaskId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class TaskCommentViewModel
{
    public int Id { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string AuthorEmail { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}

public class TaskDetailsViewModel
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public string? AssigneeName { get; set; }

    public bool CurrentUserCanEdit { get; set; }

    public bool CurrentUserCanDelete { get; set; }

    public bool ProjectIsArchived { get; set; }

    public IReadOnlyList<TaskCommentViewModel> Comments { get; set; } = [];

    public AddTaskCommentViewModel NewComment { get; set; } = new();
}
