using System.ComponentModel.DataAnnotations;
using TaskHub.Dtos.Auth;
using TaskHub.Dtos.Tasks;
using TaskHub.Models.Enums;

namespace TaskHub.Dtos.Projects;

public class CreateProjectRequestDto
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}

public class UpdateProjectRequestDto
{
    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}

public class AddProjectMemberRequestDto
{
    public string? UserId { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}

public class ProjectMemberDto
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public ProjectRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}

public class ProjectSummaryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public UserSummaryDto Owner { get; set; } = new();

    public int MemberCount { get; set; }

    public int TaskCount { get; set; }

    public ProjectRole CurrentUserRole { get; set; }
}

public class ProjectDetailsDto : ProjectSummaryDto
{
    public IReadOnlyList<ProjectMemberDto> Members { get; set; } = [];

    public IReadOnlyList<TaskSummaryDto> Tasks { get; set; } = [];
}
