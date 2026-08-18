using System.ComponentModel.DataAnnotations;

namespace TaskHub.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "TaskHub";

    [Required]
    public string Audience { get; set; } = "TaskHub.Client";

    public string SigningKey { get; set; } = string.Empty;

    [Range(5, 1440)]
    public int ExpirationMinutes { get; set; } = 120;
}
