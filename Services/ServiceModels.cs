using TaskHub.Models.Enums;

namespace TaskHub.Services;

public record ProjectCreateInput(string Name, string? Description);

public record ProjectUpdateInput(string Name, string? Description);

public record AddProjectMemberInput(string? UserId, string? Email);

public record TaskCreateInput(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssignedToUserId);

public record TaskUpdateInput(
    string Title,
    string? Description,
    DateTime? DueDateUtc,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssignedToUserId);

public record CommentCreateInput(string Content);

public record CommentUpdateInput(string Content);
