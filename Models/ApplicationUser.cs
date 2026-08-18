using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TaskHub.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    [NotMapped]
    public string DisplayName => string.Join(" ", new[] { FirstName, LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public ICollection<Project> OwnedProjects { get; set; } = [];

    public ICollection<ProjectMembership> ProjectMemberships { get; set; } = [];

    public ICollection<TaskItem> CreatedTasks { get; set; } = [];

    public ICollection<TaskItem> AssignedTasks { get; set; } = [];

    public ICollection<TaskComment> TaskComments { get; set; } = [];
}
