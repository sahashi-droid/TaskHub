using TaskHub.Models;

namespace TaskHub.Services;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> GetVisibleProjectsAsync(string userId);

    Task<ServiceResult<Project>> GetProjectDetailsAsync(int projectId, string userId);

    Task<ServiceResult<Project>> CreateProjectAsync(string userId, ProjectCreateInput input);

    Task<ServiceResult<Project>> UpdateProjectAsync(int projectId, string userId, ProjectUpdateInput input);

    Task<ServiceResult> ArchiveProjectAsync(int projectId, string userId);

    Task<ServiceResult<ProjectMembership>> AddMemberAsync(int projectId, string ownerUserId, AddProjectMemberInput input);

    Task<ServiceResult> RemoveMemberAsync(int projectId, string ownerUserId, string memberUserId);

    Task<bool> IsProjectMemberAsync(int projectId, string userId);

    Task<bool> IsProjectOwnerAsync(int projectId, string userId);
}
