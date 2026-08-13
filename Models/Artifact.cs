namespace LocusAPI.Models;

public record Artifact
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? SourceUrl { get; init; }
    public string? Institution { get; init; }
    public string? Year { get; init; }
}