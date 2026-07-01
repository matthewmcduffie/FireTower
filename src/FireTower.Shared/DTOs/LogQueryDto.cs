namespace FireTower.Shared.DTOs;

/// <summary>
/// Filter parameters for retrieving a page of log entries via <c>GetLogs</c>.
/// </summary>
public sealed class LogQueryDto
{
    public string? Component { get; init; }
    public string? MinimumLevel { get; init; }
    public string? SearchText { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; } = 200;
}
