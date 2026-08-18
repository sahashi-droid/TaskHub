using TaskHub.Models;
using TaskHub.Models.Enums;

namespace TaskHub.Services;

public interface ITaskService
{
    Task<ServiceResult<IReadOnlyList<TaskItem>>> GetProjectTasksAsync(int projectId, string userId, TaskItemStatus? status = null);

    Task<ServiceResult<TaskItem>> GetTaskDetailsAsync(int taskId, string userId);

    Task<ServiceResult<TaskItem>> CreateTaskAsync(int projectId, string userId, TaskCreateInput input);

    Task<ServiceResult<TaskItem>> UpdateTaskAsync(int taskId, string userId, TaskUpdateInput input);

    Task<ServiceResult> DeleteTaskAsync(int taskId, string userId);
}
