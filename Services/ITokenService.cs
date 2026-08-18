using TaskHub.Models;

namespace TaskHub.Services;

public record JwtTokenResult(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    JwtTokenResult CreateToken(ApplicationUser user);
}
