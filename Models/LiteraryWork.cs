namespace LocusAPI.Models;

public record LiteraryWork
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? AuthorBorn { get; init; }
    public string? Url { get; init; }
}