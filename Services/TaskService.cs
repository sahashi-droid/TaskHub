using Microsoft.EntityFrameworkCore;
using TaskHub.Data;
using TaskHub.Models;
using TaskHub.Models.Enums;

namespace TaskHub.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _dbContext;

    public TaskService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult<IReadOnlyList<TaskItem>>> GetProjectTasksAsync(int projectId, string userId, TaskItemStatus? status = null)
    {
        var accessResult = await GetProjectForMembershipAsync(projectId, userId);
        if (!accessResult.Succeeded)
        {
            return ServiceResult<IReadOnlyList<TaskItem>>.Failure(accessResult.ErrorType!.Value, accessResult.ErrorMessage!, accessResult.ValidationErrors);
        }

        var tasksQuery = _dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .Include(task => task.AssignedToUser)
            .Include(task => task.CreatedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            tasksQuery = tasksQuery.Where(task => task.Status == status.Value);
        }

        var tasks = await tasksQuery
            .OrderBy(task => task.Status)
            .ThenBy(task => task.DueDateUtc ?? DateTime.MaxValue)
            .ThenByDescending(task => task.UpdatedAtUtc)
            .ToListAsync();

        return ServiceResult<IReadOnlyList<TaskItem>>.Success(tasks);
    }

    public async Task<ServiceResult<TaskItem>> GetTaskDetailsAsync(int taskId, string userId)
    {
        var taskItem = await _dbContext.TaskItems
            .AsNoTracking()
            .AsSplitQuery()
            .Include(task => task.Project)
                .ThenInclude(project => project.Owner)
            .Include(task => task.Project)
                .ThenInclude(project => project.Memberships)
                    .ThenInclude(membership => membership.User)
            .Include(task => task.AssignedToUser)
            .Include(task => task.CreatedByUser)
            .Include(task => task.Comments)
                .ThenInclude(comment => comment.AuthorUser)
            .FirstOrDefaultAsync(task => task.Id == taskId);

        if (taskItem is null)
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.NotFound, "Task not found.");
        }

        if (!taskItem.Project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.Forbidden, "You do not have access to this task.");
        }

        taskItem.Comments = taskItem.Comments
            .OrderBy(comment => comment.CreatedAtUtc)
            .ToList();

        return ServiceResult<TaskItem>.Success(taskItem);
    }

    public async Task<ServiceResult<TaskItem>> CreateTaskAsync(int projectId, string userId, TaskCreateInput input)
    {
        var projectResult = await GetProjectForMembershipAsync(projectId, userId, includeUsers: true, asTracking: true);
        if (!projectResult.Succeeded)
        {
            return ServiceResult<TaskItem>.Failure(projectResult.ErrorType!.Value, projectResult.ErrorMessage!, projectResult.ValidationErrors);
        }

        var project = projectResult.Value!;
        if (project.IsArchived)
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.Conflict, "Archived projects do not accept new tasks.");
        }

        var validationErrors = ValidateTask(input.Title, input.Description, input.Status, input.Priority);
        if (validationErrors is not null)
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.Validation, "Task validation failed.", validationErrors);
        }

        var assigneeResult = ValidateAssignee(project, input.AssignedToUserId);
        if (!assigneeResult.Succeeded)
        {
            return ServiceResult<TaskItem>.Failure(assigneeResult.ErrorType!.Value, assigneeResult.ErrorMessage!, assigneeResult.ValidationErrors);
        }

        var utcNow = DateTime.UtcNow;
        var taskItem = new TaskItem
        {
            ProjectId = project.Id,
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            DueDateUtc = input.DueDateUtc,
            Status = input.Status,
            Priority = input.Priority,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            CreatedByUserId = userId,
            AssignedToUserId = NormalizeNullableValue(input.AssignedToUserId),
            IsDeleted = false
        };

        _dbContext.TaskItems.Add(taskItem);
        project.UpdatedAtUtc = utcNow;
        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(taskItem).Reference(item => item.Project).LoadAsync();
        await _dbContext.Entry(taskItem).Reference(item => item.CreatedByUser).LoadAsync();
        if (!string.IsNullOrWhiteSpace(taskItem.AssignedToUserId))
        {
            await _dbContext.Entry(taskItem).Reference(item => item.AssignedToUser).LoadAsync();
        }

        return ServiceResult<TaskItem>.Success(taskItem);
    }

    public async Task<ServiceResult<TaskItem>> UpdateTaskAsync(int taskId, string userId, TaskUpdateInput input)
    {
        var taskItem = await _dbContext.TaskItems
            .Include(task => task.Project)
                .ThenInclude(project => project.Memberships)
                    .ThenInclude(membership => membership.User)
            .FirstOrDefaultAsync(task => task.Id == taskId);

        if (taskItem is null)
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.NotFound, "Task not found.");
        }

        if (!taskItem.Project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.Forbidden, "Only project members can update this task.");
        }

        if (taskItem.Project.IsArchived)
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.Conflict, "Archived projects do not accept task updates.");
        }

        var validationErrors = ValidateTask(input.Title, input.Description, input.Status, input.Priority);
        if (validationErrors is not null)
        {
            return ServiceResult<TaskItem>.Failure(ServiceErrorType.Validation, "Task validation failed.", validationErrors);
        }

        var assigneeResult = ValidateAssignee(taskItem.Project, input.AssignedToUserId);
        if (!assigneeResult.Succeeded)
        {
            return ServiceResult<TaskItem>.Failure(assigneeResult.ErrorType!.Value, assigneeResult.ErrorMessage!, assigneeResult.ValidationErrors);
        }

        taskItem.Title = input.Title.Trim();
        taskItem.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        taskItem.DueDateUtc = input.DueDateUtc;
        taskItem.Status = input.Status;
        taskItem.Priority = input.Priority;
        taskItem.AssignedToUserId = NormalizeNullableValue(input.AssignedToUserId);
        taskItem.UpdatedAtUtc = DateTime.UtcNow;
        taskItem.Project.UpdatedAtUtc = taskItem.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(taskItem).Reference(item => item.AssignedToUser).LoadAsync();
        await _dbContext.Entry(taskItem).Reference(item => item.CreatedByUser).LoadAsync();

        return ServiceResult<TaskItem>.Success(taskItem);
    }

    public async Task<ServiceResult> DeleteTaskAsync(int taskId, string userId)
    {
        var taskItem = await _dbContext.TaskItems
            .Include(task => task.Project)
                .ThenInclude(project => project.Memberships)
            .FirstOrDefaultAsync(task => task.Id == taskId);

        if (taskItem is null)
        {
            return ServiceResult.Failure(ServiceErrorType.NotFound, "Task not found.");
        }

        if (!taskItem.Project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult.Failure(ServiceErrorType.Forbidden, "Only project members can delete this task.");
        }

        if (taskItem.CreatedByUserId != userId && taskItem.Project.OwnerId != userId)
        {
            return ServiceResult.Failure(ServiceErrorType.Forbidden, "Only the task creator or project owner can delete this task.");
        }

        taskItem.IsDeleted = true;
        taskItem.UpdatedAtUtc = DateTime.UtcNow;
        taskItem.Project.UpdatedAtUtc = taskItem.UpdatedAtUtc;

        await _dbContext.SaveChangesAsync();

        return ServiceResult.Success();
    }

    private async Task<ServiceResult<Project>> GetProjectForMembershipAsync(
        int projectId,
        string userId,
        bool includeUsers = false,
        bool asTracking = false)
    {
        var query = asTracking ? _dbContext.Projects.AsQueryable() : _dbContext.Projects.AsNoTracking();
        query = query.Include(project => project.Memberships);

        if (includeUsers)
        {
            query = query.Include(project => project.Memberships)
                .ThenInclude(membership => membership.User);
        }

        var project = await query.FirstOrDefaultAsync(item => item.Id == projectId);

        if (project is null)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.NotFound, "Project not found.");
        }

        if (!project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.Forbidden, "You do not have access to this project.");
        }

        return ServiceResult<Project>.Success(project);
    }

    private static ServiceResult ValidateAssignee(Project project, string? assignedToUserId)
    {
        var normalizedAssigneeId = NormalizeNullableValue(assignedToUserId);
        if (normalizedAssigneeId is null)
        {
            return ServiceResult.Success();
        }

        var membership = project.Memberships.FirstOrDefault(item => item.UserId == normalizedAssigneeId);
        if (membership is null)
        {
            return ServiceResult.Failure(
                ServiceErrorType.Validation,
                "Assigned user must be a project member.",
                new Dictionary<string, string[]> { ["AssignedToUserId"] = ["Assigned user must belong to the project."] });
        }

        if (membership.User is null || !membership.User.IsActive)
        {
            return ServiceResult.Failure(
                ServiceErrorType.Validation,
                "Assigned user must be active.",
                new Dictionary<string, string[]> { ["AssignedToUserId"] = ["Only active users can be assigned to tasks."] });
        }

        return ServiceResult.Success();
    }

    private static IReadOnlyDictionary<string, string[]>? ValidateTask(
        string title,
        string? description,
        TaskItemStatus status,
        TaskPriority priority)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["Title"] = ["Task title is required."];
        }
        else if (title.Trim().Length > 200)
        {
            errors["Title"] = ["Task title must be 200 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 4000)
        {
            errors["Description"] = ["Task description must be 4000 characters or fewer."];
        }

        if (!Enum.IsDefined(status))
        {
            errors["Status"] = ["A valid task status is required."];
        }

        if (!Enum.IsDefined(priority))
        {
            errors["Priority"] = ["A valid task priority is required."];
        }

        return errors.Count > 0 ? errors : null;
    }

    private static string? NormalizeNullableValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
