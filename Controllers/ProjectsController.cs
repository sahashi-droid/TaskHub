using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Models;
using TaskHub.Models.Enums;
using TaskHub.Services;
using TaskHub.ViewModels.Projects;

namespace TaskHub.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projects = await _projectService.GetVisibleProjectsAsync(userId);
        var model = new ProjectIndexViewModel
        {
            Projects = projects.Select(project =>
            {
                var currentMembership = project.Memberships.First(membership => membership.UserId == userId);
                return new ProjectListItemViewModel
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    IsArchived = project.IsArchived,
                    OwnerName = project.Owner.DisplayName,
                    CurrentUserRole = currentMembership.Role,
                    MemberCount = project.Memberships.Count,
                    TaskCount = project.Tasks.Count,
                    UpdatedAtUtc = project.UpdatedAtUtc
                };
            }).ToList()
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

        var result = await _projectService.GetProjectDetailsAsync(id, userId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return View(MapProjectDetails(result.Value!, userId));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProjectFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectFormViewModel model)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _projectService.CreateProjectAsync(userId, new ProjectCreateInput(model.Name, model.Description));
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(model);
        }

        TempData["SuccessMessage"] = "Project created successfully.";
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

        var result = await _projectService.GetProjectDetailsAsync(id, userId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        if (result.Value!.OwnerId != userId)
        {
            return Forbid();
        }

        var model = new ProjectFormViewModel
        {
            Id = result.Value!.Id,
            Name = result.Value.Name,
            Description = result.Value.Description
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProjectFormViewModel model)
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

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _projectService.UpdateProjectAsync(model.Id.Value, userId, new ProjectUpdateInput(model.Name, model.Description));
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            return View(model);
        }

        TempData["SuccessMessage"] = "Project updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Members(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var projectResult = await _projectService.GetProjectDetailsAsync(id, userId);
        if (!projectResult.Succeeded)
        {
            return HandleFailure(projectResult);
        }

        if (projectResult.Value!.OwnerId != userId)
        {
            return Forbid();
        }

        return View(BuildMembersViewModel(projectResult.Value));
    }

    [HttpPost]
    public async Task<IActionResult> AddMember(ManageProjectMembersViewModel model)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var currentProjectResult = await _projectService.GetProjectDetailsAsync(model.ProjectId, userId);
        if (!currentProjectResult.Succeeded)
        {
            return HandleFailure(currentProjectResult);
        }

        if (currentProjectResult.Value!.OwnerId != userId)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = BuildMembersViewModel(currentProjectResult.Value, model.AddMemberEmail);
            return View("Members", invalidModel);
        }

        var result = await _projectService.AddMemberAsync(model.ProjectId, userId, new AddProjectMemberInput(null, model.AddMemberEmail));
        if (!result.Succeeded)
        {
            AddServiceErrors(result);
            var failedModel = BuildMembersViewModel(currentProjectResult.Value, model.AddMemberEmail);
            return View("Members", failedModel);
        }

        TempData["SuccessMessage"] = $"{result.Value!.User.DisplayName} was added to the project.";
        return RedirectToAction(nameof(Members), new { id = model.ProjectId });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveMember(int projectId, string memberUserId)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _projectService.RemoveMemberAsync(projectId, userId, memberUserId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Member removed from the project.";
        }

        return RedirectToAction(nameof(Members), new { id = projectId });
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await _projectService.ArchiveProjectAsync(id, userId);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Project archived successfully.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static ManageProjectMembersViewModel BuildMembersViewModel(Project project, string? addMemberEmail = null)
    {
        return new ManageProjectMembersViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            ProjectDescription = project.Description,
            IsArchived = project.IsArchived,
            AddMemberEmail = addMemberEmail,
            Members = project.Memberships.Select(membership => new ProjectMemberViewModel
            {
                UserId = membership.UserId,
                DisplayName = membership.User.DisplayName,
                Email = membership.User.Email ?? string.Empty,
                Role = membership.Role,
                JoinedAtUtc = membership.JoinedAtUtc
            }).ToList()
        };
    }

    private ProjectDetailsViewModel MapProjectDetails(Project project, string userId)
    {
        return new ProjectDetailsViewModel
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            CreatedAtUtc = project.CreatedAtUtc,
            UpdatedAtUtc = project.UpdatedAtUtc,
            OwnerId = project.OwnerId,
            OwnerName = project.Owner.DisplayName,
            IsArchived = project.IsArchived,
            CurrentUserIsOwner = project.OwnerId == userId,
            Members = project.Memberships.Select(membership => new ProjectMemberViewModel
            {
                UserId = membership.UserId,
                DisplayName = membership.User.DisplayName,
                Email = membership.User.Email ?? string.Empty,
                Role = membership.Role,
                JoinedAtUtc = membership.JoinedAtUtc
            }).ToList(),
            Tasks = project.Tasks.Select(task => new ProjectTaskSummaryViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Status = task.Status,
                Priority = task.Priority,
                AssigneeName = task.AssignedToUser?.DisplayName,
                DueDateUtc = task.DueDateUtc,
                UpdatedAtUtc = task.UpdatedAtUtc
            }).ToList()
        };
    }

    private IActionResult HandleFailure(ServiceResult result)
    {
        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(),
            ServiceErrorType.Forbidden => Forbid(),
            _ => RedirectToAction(nameof(Index))
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
