namespace TaskHub.Models;

public class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string OwnerId { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public ApplicationUser Owner { get; set; } = null!;

    public ICollection<ProjectMembership> Memberships { get; set; } = [];

    public ICollection<TaskItem> Tasks { get; set; } = [];
}
