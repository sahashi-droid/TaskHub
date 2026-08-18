using System.ComponentModel.DataAnnotations;
using TaskHub.Dtos.Auth;

namespace TaskHub.Dtos.Comments;

public class CreateCommentRequestDto
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentRequestDto : CreateCommentRequestDto
{
}

public class CommentDto
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public UserSummaryDto Author { get; set; } = new();
}
