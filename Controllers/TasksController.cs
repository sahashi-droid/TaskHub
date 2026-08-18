using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskHub.Models;
using TaskHub.Models.Enums;
using TaskHub.Services;
using TaskHub.ViewModels.Tasks;

namespace TaskHub.Controllers;

[Authorize]
public class TasksController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly ICommentService _commentService;

    public TasksController(IProjectService projectService, ITaskService taskService, ICommentService commentService)
    {
        _projectService = projectService;
        _taskService = taskService;
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int projectId, TaskItemStatus? status)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projectResult = await _projectService.GetProjectDetailsAsync(projectId, userId);
        if (!projectResult.Succeeded)
        {
            return HandleFailure(projectResult);
        }

        var tasksResult = await _taskService.GetProjectTasksAsync(projectId, userId, status);
        if (!tasksResult.Succeeded)
        {
            return HandleFailure(tasksResult);
        }

        var model = new TaskIndexViewModel
        {
            ProjectId = projectId,
            ProjectName = projectResult.Value!.Name,
            IsArchived = projectResult.Value.IsArchived,
            SelectedStatus = status,
            StatusOptions = BuildStatusFilterOptions(status),
            Tasks = tasksResult.Value!.Select(MapTaskListItem).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var taskResult = await _taskService.GetTaskDetailsAsync(id, userId);
        if (!taskResult.Succeeded)
        {
            return HandleFailure(taskResult);
        }

        return View(MapTaskDetails(taskResult.Value!, userId));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int projectId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projectResult = await _projectService.GetProjectDetailsAsync(projectId, userId);
        if (!projectResult.Succeeded)
        {
            return HandleFailure(projectResult);
        }

        if (projectResult.Value!.IsArchived)
        {
            TempData["ErrorMessage"] = "Archived projects do not accept new tasks.";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }

        var model = BuildTaskFormModel(projectResult.Value, new TaskFormViewModel { ProjectId = projectId });
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskFormViewModel model)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projectResult = await _projectService.GetProjectDetailsAsync(model.ProjectId, userId);
        if (!projectResult.Succeeded)
        {
            return HandleFailure(projectResult);
        }

        if (!ModelState.IsValid)
        {
            return View(BuildTaskFormModel(projectResult.Value!, model));
        }

        var result = await _taskService.CreateTaskAsync(
            model.ProjectId,
            userId,
            new TaskCreateInput(model.Title, model.Description, model.DueDateUtc, model.Status, model.Priority, model.AssignedToUserId));

        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(BuildTaskFormModel(projectResult.Value!, model));
        }

        TempData["SuccessMessage"] = "Task created successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _taskService.GetTaskDetailsAsync(id, userId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        var task = result.Value!;
        if (task.Project.IsArchived)
        {
            TempData["ErrorMessage"] = "Archived projects do not accept task updates.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = BuildTaskFormModel(task.Project, new TaskFormViewModel
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Title = task.Title,
            Description = task.Description,
            DueDateUtc = task.DueDateUtc,
            Status = task.Status,
            Priority = task.Priority,
            AssignedToUserId = task.AssignedToUserId
        });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(TaskFormViewModel model)
    {
        if (model.Id is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projectResult = await _projectService.GetProjectDetailsAsync(model.ProjectId, userId);
        if (!projectResult.Succeeded)
        {
            return HandleFailure(projectResult);
        }

        if (!ModelState.IsValid)
        {
            return View(BuildTaskFormModel(projectResult.Value!, model));
        }

        var result = await _taskService.UpdateTaskAsync(
            model.Id.Value,
            userId,
            new TaskUpdateInput(model.Title, model.Description, model.DueDateUtc, model.Status, model.Priority, model.AssignedToUserId));

        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(BuildTaskFormModel(projectResult.Value!, model));
        }

        TempData["SuccessMessage"] = "Task updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id.Value });
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(AddTaskCommentViewModel model)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildTaskDetailsViewModelAsync(model.TaskId, userId, model.Content);
            return invalidModel is null ? NotFound() : View("Details", invalidModel);
        }

        var result = await _commentService.AddCommentAsync(model.TaskId, userId, new CommentCreateInput(model.Content));
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            var failedModel = await BuildTaskDetailsViewModelAsync(model.TaskId, userId, model.Content);
            return failedModel is null ? NotFound() : View("Details", failedModel);
        }

        TempData["SuccessMessage"] = "Comment added successfully.";
        return RedirectToAction(nameof(Details), new { id = model.TaskId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var taskResult = await _taskService.GetTaskDetailsAsync(id, userId);
        if (!taskResult.Succeeded)
        {
            return HandleFailure(taskResult);
        }

        var deleteResult = await _taskService.DeleteTaskAsync(id, userId);
        if (!deleteResult.Succeeded)
        {
            TempData["ErrorMessage"] = deleteResult.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Task deleted successfully.";
        return RedirectToAction("Details", "Projects", new { id = taskResult.Value!.ProjectId });
    }

    private async Task<TaskDetailsViewModel?> BuildTaskDetailsViewModelAsync(int taskId, string userId, string? draftComment = null)
    {
        var result = await _taskService.GetTaskDetailsAsync(taskId, userId);
        if (!result.Succeeded)
        {
            return null;
        }

        return MapTaskDetails(result.Value!, userId, draftComment);
    }

    private static TaskFormViewModel BuildTaskFormModel(Project project, TaskFormViewModel model)
    {
        model.ProjectName = project.Name;
        model.ProjectIsArchived = project.IsArchived;
        model.AssigneeOptions = project.Memberships
            .Where(membership => membership.User.IsActive)
            .OrderByDescending(membership => membership.Role == ProjectRole.Owner)
            .ThenBy(membership => membership.User.DisplayName)
            .Select(membership => new SelectListItem(membership.User.DisplayName, membership.UserId))
            .Prepend(new SelectListItem("Unassigned", string.Empty))
            .ToList();

        model.StatusOptions = Enum.GetValues<TaskItemStatus>()
            .Select(status => new SelectListItem(status.ToString(), status.ToString(), status == model.Status))
            .ToList();

        model.PriorityOptions = Enum.GetValues<TaskPriority>()
            .Select(priority => new SelectListItem(priority.ToString(), priority.ToString(), priority == model.Priority))
            .ToList();

        return model;
    }

    private static IReadOnlyList<SelectListItem> BuildStatusFilterOptions(TaskItemStatus? status)
    {
        return new List<SelectListItem>
        {
            new("All statuses", string.Empty, !status.HasValue)
        }.Concat(
            Enum.GetValues<TaskItemStatus>()
                .Select(item => new SelectListItem(item.ToString(), item.ToString(), item == status)))
            .ToList();
    }

    private static TaskListItemViewModel MapTaskListItem(TaskItem task)
    {
        return new TaskListItemViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Status = task.Status,
            Priority = task.Priority,
            AssigneeName = task.AssignedToUser?.DisplayName,
            CreatedByName = task.CreatedByUser.DisplayName,
            DueDateUtc = task.DueDateUtc,
            UpdatedAtUtc = task.UpdatedAtUtc
        };
    }

    private static TaskDetailsViewModel MapTaskDetails(TaskItem task, string userId, string? draftComment = null)
    {
        var canDelete = task.CreatedByUserId == userId || task.Project.OwnerId == userId;

        return new TaskDetailsViewModel
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ProjectName = task.Project.Name,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDateUtc = task.DueDateUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc,
            CreatedByName = task.CreatedByUser.DisplayName,
            AssigneeName = task.AssignedToUser?.DisplayName,
            CurrentUserCanEdit = !task.Project.IsArchived,
            CurrentUserCanDelete = canDelete,
            ProjectIsArchived = task.Project.IsArchived,
            Comments = task.Comments.Select(comment => new TaskCommentViewModel
            {
                Id = comment.Id,
                AuthorName = comment.AuthorUser.DisplayName,
                AuthorEmail = comment.AuthorUser.Email ?? string.Empty,
                Content = comment.Content,
                CreatedAtUtc = comment.CreatedAtUtc,
                UpdatedAtUtc = comment.UpdatedAtUtc
            }).ToList(),
            NewComment = new AddTaskCommentViewModel
            {
                TaskId = task.Id,
                Content = draftComment ?? string.Empty
            }
        };
    }

    private IActionResult HandleFailure(ServiceResult result)
    {
        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(),
            ServiceErrorType.Forbidden => Forbid(),
            _ => RedirectToAction("Index", "Projects")
        };
    }

    private void AddServiceErrors(ServiceResult result)
    {
        if (result.ValidationErrors is not null)
        {
            foreach (var validationError in result.ValidationErrors)
            {
                foreach (var error in validationError.Value)
                {
                    ModelState.AddModelError(validationError.Key, error);
                }
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
        }
    }
}
