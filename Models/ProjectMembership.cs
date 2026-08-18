using TaskHub.Models.Enums;

namespace TaskHub.Models;

public class ProjectMembership
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ProjectRole Role { get; set; }

    public DateTime JoinedAtUtc { get; set; }

    public Project Project { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}
