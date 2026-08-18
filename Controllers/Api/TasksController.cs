using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Dtos.Auth;
using TaskHub.Dtos.Comments;
using TaskHub.Dtos.Tasks;
using TaskHub.Models;
using TaskHub.Services;

namespace TaskHub.Controllers.Api;

[IgnoreAntiforgeryToken]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api")]
public class TasksController : ApiControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("projects/{projectId:int}/tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectTasks(int projectId)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetProjectTasksAsync(projectId, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value!.Select(MapTaskSummary).ToList());
    }

    [HttpGet("tasks/{id:int}")]
    [ProducesResponseType(typeof(TaskDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTask(int id)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.GetTaskDetailsAsync(id, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Ok(MapTaskDetails(result.Value!));
    }

    [HttpPost("projects/{projectId:int}/tasks")]
    [ProducesResponseType(typeof(TaskDetailsDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTask(int projectId, CreateTaskRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.CreateTaskAsync(
            projectId,
            CurrentUserId,
            new TaskCreateInput(request.Title, request.Description, request.DueDateUtc, request.Status, request.Priority, request.AssignedToUserId));

        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        var detailsResult = await _taskService.GetTaskDetailsAsync(result.Value!.Id, CurrentUserId);
        return CreatedAtAction(nameof(GetTask), new { id = result.Value.Id }, MapTaskDetails(detailsResult.Value!));
    }

    [HttpPut("tasks/{id:int}")]
    [ProducesResponseType(typeof(TaskDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTask(int id, UpdateTaskRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.UpdateTaskAsync(
            id,
            CurrentUserId,
            new TaskUpdateInput(request.Title, request.Description, request.DueDateUtc, request.Status, request.Priority, request.AssignedToUserId));

        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        var detailsResult = await _taskService.GetTaskDetailsAsync(id, CurrentUserId);
        return Ok(MapTaskDetails(detailsResult.Value!));
    }

    [HttpDelete("tasks/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteTask(int id)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _taskService.DeleteTaskAsync(id, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return NoContent();
    }

    private static TaskSummaryDto MapTaskSummary(TaskItem task)
        => new()
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Status = task.Status,
            Priority = task.Priority,
            DueDateUtc = task.DueDateUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            CreatedBy = MapUser(task.CreatedByUser),
            AssignedTo = task.AssignedToUser is null ? null : MapUser(task.AssignedToUser)
        };

    private static TaskDetailsDto MapTaskDetails(TaskItem task)
        => new()
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDateUtc = task.DueDateUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            CreatedBy = MapUser(task.CreatedByUser),
            AssignedTo = task.AssignedToUser is null ? null : MapUser(task.AssignedToUser),
            Comments = task.Comments.Select(comment => new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAtUtc = comment.CreatedAtUtc,
                UpdatedAtUtc = comment.UpdatedAtUtc,
                Author = MapUser(comment.AuthorUser)
            }).ToList()
        };

    private static UserSummaryDto MapUser(ApplicationUser user)
        => new()
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
}
