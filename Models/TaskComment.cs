namespace TaskHub.Models;

public class TaskComment
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    public string AuthorUserId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public ApplicationUser AuthorUser { get; set; } = null!;
}
