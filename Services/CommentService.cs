using Microsoft.EntityFrameworkCore;
using TaskHub.Data;
using TaskHub.Models;

namespace TaskHub.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _dbContext;

    public CommentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<IReadOnlyList<TaskComment>>> GetTaskCommentsAsync(int taskId, string userId)
    {
        var taskItem = await _dbContext.TaskItems
            .AsNoTracking()
            .Include(task => task.Project)
                .ThenInclude(project => project.Memberships)
            .FirstOrDefaultAsync(task => task.Id == taskId);

        if (taskItem is null)
        {
            return ServiceResult<IReadOnlyList<TaskComment>>.Failure(ServiceErrorType.NotFound, "Task not found.");
        }

        if (!taskItem.Project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult<IReadOnlyList<TaskComment>>.Failure(ServiceErrorType.Forbidden, "Only project members can view comments.");
        }

        var comments = await _dbContext.TaskComments
            .AsNoTracking()
            .Where(comment => comment.TaskItemId == taskId)
            .Include(comment => comment.AuthorUser)
            .OrderBy(comment => comment.CreatedAtUtc)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<TaskComment>>.Success(comments);
    }

    public async Task<ServiceResult<TaskComment>> AddCommentAsync(int taskId, string userId, CommentCreateInput input)
    {
        var validationErrors = ValidateComment(input.Content);
        if (validationErrors is not null)
        {
            return ServiceResult<TaskComment>.Failure(ServiceErrorType.Validation, "Comment validation failed.", validationErrors);
        }

        var taskItem = await _dbContext.TaskItems
            .Include(task => task.Project)
                .ThenInclude(project => project.Memberships)
            .FirstOrDefaultAsync(task => task.Id == taskId);

        if (taskItem is null)
        {
            return ServiceResult<TaskComment>.Failure(ServiceErrorType.NotFound, "Task not found.");
        }

        if (!taskItem.Project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult<TaskComment>.Failure(ServiceErrorType.Forbidden, "Only project members can comment on tasks.");
        }

        var comment = new TaskComment
        {
            TaskItemId = taskId,
            AuthorUserId = userId,
            Content = input.Content.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.TaskComments.Add(comment);
        taskItem.UpdatedAtUtc = DateTime.UtcNow;
        taskItem.Project.UpdatedAtUtc = taskItem.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(comment).Reference(item => item.AuthorUser).LoadAsync();

        return ServiceResult<TaskComment>.Success(comment);
    }

    public async Task<ServiceResult<TaskComment>> UpdateCommentAsync(int commentId, string userId, CommentUpdateInput input)
    {
        var validationErrors = ValidateComment(input.Content);
        if (validationErrors is not null)
        {
            return ServiceResult<TaskComment>.Failure(ServiceErrorType.Validation, "Comment validation failed.", validationErrors);
        }

        var comment = await _dbContext.TaskComments
            .Include(item => item.TaskItem)
                .ThenInclude(task => task.Project)
            .FirstOrDefaultAsync(item => item.Id == commentId);

        if (comment is null)
        {
            return ServiceResult<TaskComment>.Failure(ServiceErrorType.NotFound, "Comment not found.");
        }

        if (comment.AuthorUserId != userId && comment.TaskItem.Project.OwnerId != userId)
        {
            return ServiceResult<TaskComment>.Failure(ServiceErrorType.Forbidden, "Only the comment author or project owner can edit this comment.");
        }

        comment.Content = input.Content.Trim();
        comment.UpdatedAtUtc = DateTime.UtcNow;
        comment.TaskItem.UpdatedAtUtc = comment.UpdatedAtUtc.Value;
        comment.TaskItem.Project.UpdatedAtUtc = comment.UpdatedAtUtc.Value;

        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(comment).Reference(item => item.AuthorUser).LoadAsync();

        return ServiceResult<TaskComment>.Success(comment);
    }

    public async Task<ServiceResult> DeleteCommentAsync(int commentId, string userId)
    {
        var comment = await _dbContext.TaskComments
            .Include(item => item.TaskItem)
                .ThenInclude(task => task.Project)
            .FirstOrDefaultAsync(item => item.Id == commentId);

        if (comment is null)
        {
            return ServiceResult.Failure(ServiceErrorType.NotFound, "Comment not found.");
        }

        if (comment.AuthorUserId != userId && comment.TaskItem.Project.OwnerId != userId)
        {
            return ServiceResult.Failure(ServiceErrorType.Forbidden, "Only the comment author or project owner can delete this comment.");
        }

        _dbContext.TaskComments.Remove(comment);
        comment.TaskItem.UpdatedAtUtc = DateTime.UtcNow;
        comment.TaskItem.Project.UpdatedAtUtc = comment.TaskItem.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private static IReadOnlyDictionary<string, string[]>? ValidateComment(string content)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(content))
        {
            errors["Content"] = ["Comment content is required."];
        }
        else if (content.Trim().Length > 2000)
        {
            errors["Content"] = ["Comment content must be 2000 characters or fewer."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
