using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Dtos.Auth;
using TaskHub.Dtos.Projects;
using TaskHub.Dtos.Tasks;
using TaskHub.Models;
using TaskHub.Models.Enums;
using TaskHub.Services;

namespace TaskHub.Controllers.Api;
[IgnoreAntiforgeryToken]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/projects")]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects()
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var projects = await _projectService.GetVisibleProjectsAsync(CurrentUserId);
        var response = projects.Select(project => MapProjectSummary(project, CurrentUserId)).ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProject(int id)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _projectService.GetProjectDetailsAsync(id, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Ok(MapProjectDetails(result.Value!, CurrentUserId));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProject(CreateProjectRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _projectService.CreateProjectAsync(CurrentUserId, new ProjectCreateInput(request.Name, request.Description));
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        var detailsResult = await _projectService.GetProjectDetailsAsync(result.Value!.Id, CurrentUserId);
        return CreatedAtAction(nameof(GetProject), new { id = result.Value.Id }, MapProjectDetails(detailsResult.Value!, CurrentUserId));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProject(int id, UpdateProjectRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _projectService.UpdateProjectAsync(id, CurrentUserId, new ProjectUpdateInput(request.Name, request.Description));
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        var detailsResult = await _projectService.GetProjectDetailsAsync(id, CurrentUserId);
        return Ok(MapProjectDetails(detailsResult.Value!, CurrentUserId));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ArchiveProject(int id)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _projectService.ArchiveProjectAsync(id, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return NoContent();
    }

    [HttpPost("{id:int}/members")]
    [ProducesResponseType(typeof(ProjectMemberDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddMember(int id, AddProjectMemberRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _projectService.AddMemberAsync(id, CurrentUserId, new AddProjectMemberInput(request.UserId, request.Email));
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Ok(MapProjectMember(result.Value!));
    }

    [HttpDelete("{id:int}/members/{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(int id, string userId)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _projectService.RemoveMemberAsync(id, CurrentUserId, userId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return NoContent();
    }

    private static ProjectSummaryDto MapProjectSummary(Project project, string currentUserId)
    {
        return new ProjectSummaryDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            IsArchived = project.IsArchived,
            CreatedAtUtc = project.CreatedAtUtc,
            UpdatedAtUtc = project.UpdatedAtUtc,
            Owner = MapUser(project.Owner),
            MemberCount = project.Memberships.Count,
            TaskCount = project.Tasks.Count,
            CurrentUserRole = project.Memberships.First(membership => membership.UserId == currentUserId).Role
        };
    }

    private static ProjectDetailsDto MapProjectDetails(Project project, string currentUserId)
    {
        return new ProjectDetailsDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            IsArchived = project.IsArchived,
            CreatedAtUtc = project.CreatedAtUtc,
            UpdatedAtUtc = project.UpdatedAtUtc,
            Owner = MapUser(project.Owner),
            MemberCount = project.Memberships.Count,
            TaskCount = project.Tasks.Count,
            CurrentUserRole = project.Memberships.First(membership => membership.UserId == currentUserId).Role,
            Members = project.Memberships.Select(MapProjectMember).ToList(),
            Tasks = project.Tasks.Select(MapTaskSummary).ToList()
        };
    }

    private static ProjectMemberDto MapProjectMember(ProjectMembership membership)
        => new()
        {
            UserId = membership.UserId,
            DisplayName = membership.User.DisplayName,
            Email = membership.User.Email ?? string.Empty,
            Role = membership.Role,
            JoinedAtUtc = membership.JoinedAtUtc
        };

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
