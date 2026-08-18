using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskHub.Data;
using TaskHub.Models;
using TaskHub.Models.Enums;

namespace TaskHub.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<Project>> GetVisibleProjectsAsync(string userId)
    {
        return await _dbContext.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Where(project => project.Memberships.Any(membership => membership.UserId == userId))
            .Include(project => project.Owner)
            .Include(project => project.Memberships)
                .ThenInclude(membership => membership.User)
            .Include(project => project.Tasks)
            .OrderByDescending(project => project.UpdatedAtUtc)
            .ToListAsync();
    }

    public async Task<ServiceResult<Project>> GetProjectDetailsAsync(int projectId, string userId)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Owner)
            .Include(item => item.Memberships)
                .ThenInclude(membership => membership.User)
            .Include(item => item.Tasks)
                .ThenInclude(task => task.AssignedToUser)
            .Include(item => item.Tasks)
                .ThenInclude(task => task.CreatedByUser)
            .FirstOrDefaultAsync(item => item.Id == projectId);

        if (project is null)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.NotFound, "Project not found.");
        }

        if (!project.Memberships.Any(membership => membership.UserId == userId))
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.Forbidden, "You do not have access to this project.");
        }

        project.Memberships = project.Memberships
            .OrderByDescending(membership => membership.Role == ProjectRole.Owner)
            .ThenBy(membership => membership.User.DisplayName)
            .ToList();

        project.Tasks = project.Tasks
            .OrderBy(task => task.Status)
            .ThenBy(task => task.DueDateUtc ?? DateTime.MaxValue)
            .ThenByDescending(task => task.UpdatedAtUtc)
            .ToList();

        return ServiceResult<Project>.Success(project);
    }

    public async Task<ServiceResult<Project>> CreateProjectAsync(string userId, ProjectCreateInput input)
    {
        var validationErrors = ValidateProject(input.Name, input.Description);
        if (validationErrors is not null)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.Validation, "Project validation failed.", validationErrors);
        }

        var owner = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId && user.IsActive);
        if (owner is null)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.Unauthorized, "The current user is not active.");
        }

        var utcNow = DateTime.UtcNow;
        var project = new Project
        {
            Name = input.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            OwnerId = owner.Id,
            IsArchived = false,
            Memberships =
            [
                new ProjectMembership
                {
                    UserId = owner.Id,
                    Role = ProjectRole.Owner,
                    JoinedAtUtc = utcNow
                }
            ]
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        project.Owner = owner;
        project.Memberships.First().User = owner;

        return ServiceResult<Project>.Success(project);
    }

    public async Task<ServiceResult<Project>> UpdateProjectAsync(int projectId, string userId, ProjectUpdateInput input)
    {
        var validationErrors = ValidateProject(input.Name, input.Description);
        if (validationErrors is not null)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.Validation, "Project validation failed.", validationErrors);
        }

        var project = await _dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId);
        if (project is null)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.NotFound, "Project not found.");
        }

        if (project.OwnerId != userId)
        {
            return ServiceResult<Project>.Failure(ServiceErrorType.Forbidden, "Only the project owner can update this project.");
        }

        project.Name = input.Name.Trim();
        project.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _dbContext.Entry(project).Reference(item => item.Owner).LoadAsync();
        await _dbContext.Entry(project).Collection(item => item.Memberships).Query().Include(membership => membership.User).LoadAsync();
        await _dbContext.Entry(project).Collection(item => item.Tasks).LoadAsync();

        return ServiceResult<Project>.Success(project);
    }

    public async Task<ServiceResult> ArchiveProjectAsync(int projectId, string userId)
    {
        var project = await _dbContext.Projects.FirstOrDefaultAsync(item => item.Id == projectId);
        if (project is null)
        {
            return ServiceResult.Failure(ServiceErrorType.NotFound, "Project not found.");
        }

        if (project.OwnerId != userId)
        {
            return ServiceResult.Failure(ServiceErrorType.Forbidden, "Only the project owner can archive the project.");
        }

        project.IsArchived = true;
        project.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ProjectMembership>> AddMemberAsync(int projectId, string ownerUserId, AddProjectMemberInput input)
    {
        if (string.IsNullOrWhiteSpace(input.UserId) && string.IsNullOrWhiteSpace(input.Email))
        {
            return ServiceResult<ProjectMembership>.Failure(
                ServiceErrorType.Validation,
                "A user identifier or email is required.",
                new Dictionary<string, string[]> { ["User"] = ["A user id or email address is required."] });
        }

        var project = await _dbContext.Projects
            .Include(item => item.Memberships)
            .FirstOrDefaultAsync(item => item.Id == projectId);

        if (project is null)
        {
            return ServiceResult<ProjectMembership>.Failure(ServiceErrorType.NotFound, "Project not found.");
        }

        if (project.OwnerId != ownerUserId)
        {
            return ServiceResult<ProjectMembership>.Failure(ServiceErrorType.Forbidden, "Only the project owner can manage members.");
        }

        ApplicationUser? user = null;
        if (!string.IsNullOrWhiteSpace(input.UserId))
        {
            user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == input.UserId.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(input.Email))
        {
            user = await _userManager.FindByEmailAsync(input.Email.Trim());
        }

        if (user is null)
        {
            return ServiceResult<ProjectMembership>.Failure(ServiceErrorType.NotFound, "The specified user was not found.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<ProjectMembership>.Failure(
                ServiceErrorType.Validation,
                "The specified user is inactive.",
                new Dictionary<string, string[]> { ["User"] = ["Only active users can be added to projects."] });
        }

        if (project.Memberships.Any(membership => membership.UserId == user.Id))
        {
            return ServiceResult<ProjectMembership>.Failure(ServiceErrorType.Conflict, "That user is already a project member.");
        }

        var membership = new ProjectMembership
        {
            ProjectId = project.Id,
            UserId = user.Id,
            Role = ProjectRole.Member,
            JoinedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProjectMemberships.Add(membership);
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        membership.User = user;
        membership.Project = project;

        return ServiceResult<ProjectMembership>.Success(membership);
    }

    public async Task<ServiceResult> RemoveMemberAsync(int projectId, string ownerUserId, string memberUserId)
    {
        var project = await _dbContext.Projects
            .Include(item => item.Memberships)
            .FirstOrDefaultAsync(item => item.Id == projectId);

        if (project is null)
        {
            return ServiceResult.Failure(ServiceErrorType.NotFound, "Project not found.");
        }

        if (project.OwnerId != ownerUserId)
        {
            return ServiceResult.Failure(ServiceErrorType.Forbidden, "Only the project owner can remove members.");
        }

        if (project.OwnerId == memberUserId)
        {
            return ServiceResult.Failure(ServiceErrorType.Validation, "The project owner cannot be removed from their own project.");
        }

        var membership = project.Memberships.FirstOrDefault(item => item.UserId == memberUserId);
        if (membership is null)
        {
            return ServiceResult.Failure(ServiceErrorType.NotFound, "Membership not found.");
        }

        _dbContext.ProjectMemberships.Remove(membership);
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public Task<bool> IsProjectMemberAsync(int projectId, string userId)
        => _dbContext.ProjectMemberships.AnyAsync(membership => membership.ProjectId == projectId && membership.UserId == userId);

    public Task<bool> IsProjectOwnerAsync(int projectId, string userId)
        => _dbContext.Projects.AnyAsync(project => project.Id == projectId && project.OwnerId == userId);

    private static IReadOnlyDictionary<string, string[]>? ValidateProject(string name, string? description)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["Name"] = ["Project name is required."];
        }
        else if (name.Trim().Length > 150)
        {
            errors["Name"] = ["Project name must be 150 characters or fewer."];
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 2000)
        {
            errors["Description"] = ["Project description must be 2000 characters or fewer."];
        }

        return errors.Count > 0 ? errors : null;
    }
}
