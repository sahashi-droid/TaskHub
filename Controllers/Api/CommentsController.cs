using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskHub.Dtos.Auth;
using TaskHub.Dtos.Comments;
using TaskHub.Models;
using TaskHub.Services;

namespace TaskHub.Controllers.Api;
[IgnoreAntiforgeryToken]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api")]
public class CommentsController : ApiControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("tasks/{taskId:int}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<CommentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComments(int taskId)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _commentService.GetTaskCommentsAsync(taskId, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Ok(result.Value!.Select(MapComment).ToList());
    }

    [HttpPost("tasks/{taskId:int}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateComment(int taskId, CreateCommentRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _commentService.AddCommentAsync(taskId, CurrentUserId, new CommentCreateInput(request.Content));
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Created($"/api/comments/{result.Value!.Id}", MapComment(result.Value));
    }

    [HttpPut("comments/{id:int}")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateComment(int id, UpdateCommentRequestDto request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _commentService.UpdateCommentAsync(id, CurrentUserId, new CommentUpdateInput(request.Content));
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return Ok(MapComment(result.Value!));
    }

    [HttpDelete("comments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var result = await _commentService.DeleteCommentAsync(id, CurrentUserId);
        if (!result.Succeeded)
        {
            return HandleFailure(result);
        }

        return NoContent();
    }

    private static CommentDto MapComment(TaskComment comment)
        => new()
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpdatedAtUtc = comment.UpdatedAtUtc,
            Author = MapUser(comment.AuthorUser)
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
