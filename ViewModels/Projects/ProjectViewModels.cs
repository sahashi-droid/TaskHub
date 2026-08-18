using System.ComponentModel.DataAnnotations;
using TaskHub.Models.Enums;

namespace TaskHub.ViewModels.Projects;

public class ProjectIndexViewModel
{
    public IReadOnlyList<ProjectListItemViewModel> Projects { get; set; } = [];
}

public class ProjectListItemViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsArchived { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public ProjectRole CurrentUserRole { get; set; }

    public int MemberCount { get; set; }

    public int TaskCount { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class ProjectFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}

public class ProjectMemberViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ProjectRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}

public class ProjectTaskSummaryViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public TaskItemStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public string? AssigneeName { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

public class ProjectDetailsViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public bool CurrentUserIsOwner { get; set; }

    public IReadOnlyList<ProjectMemberViewModel> Members { get; set; } = [];

    public IReadOnlyList<ProjectTaskSummaryViewModel> Tasks { get; set; } = [];
}

public class ManageProjectMembersViewModel
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string? ProjectDescription { get; set; }

    public bool IsArchived { get; set; }

    [Display(Name = "Invite by email")]
    [EmailAddress]
    public string? AddMemberEmail { get; set; }

    public IReadOnlyList<ProjectMemberViewModel> Members { get; set; } = [];
}
