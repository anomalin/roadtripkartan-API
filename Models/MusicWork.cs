namespace LocusAPI.Models;

public record MusicWork
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Composer { get; init; }
    public string? Date {get; init;}
    public List<string> Tags { get; init; } = new();
}