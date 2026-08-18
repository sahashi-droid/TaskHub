using TaskHub.Models;

namespace TaskHub.Services;

public interface ICommentService
{
    Task<ServiceResult<IReadOnlyList<TaskComment>>> GetTaskCommentsAsync(int taskId, string userId);

    Task<ServiceResult<TaskComment>> AddCommentAsync(int taskId, string userId, CommentCreateInput input);

    Task<ServiceResult<TaskComment>> UpdateCommentAsync(int commentId, string userId, CommentUpdateInput input);

    Task<ServiceResult> DeleteCommentAsync(int commentId, string userId);
}
