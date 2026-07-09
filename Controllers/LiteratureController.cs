using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using LocusAPI.Models;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LocusAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LiteratureController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public LiteratureController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Litteraturbanken");
    }

    [HttpGet]
    public async Task<IActionResult> GetByDateRange([FromQuery] int fromYear, [FromQuery] int toYear)
    {
        if (fromYear <= 0 || toYear <= 0 || fromYear > toYear)
            return BadRequest("Invalid date range.");

        var random = new Random(fromYear + toYear);
        var startOffset = random.Next(0, 50);

        var url = "https://litteraturbanken.se/api/list_all/etext" +
                  $"?from={startOffset}&to={startOffset + 300}" +
                  "&include=authors,title,titlepath";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Error contacting Litteraturbanken");

        var json = await response.Content.ReadAsStringAsync();
        var results = ParseAndFilter(json, fromYear, toYear);

        return Ok(results);
    }


    [HttpGet("debug")]
    public async Task<IActionResult> Debug()
    {
        var random = new Random();
        var startOffset = random.Next(0, 50);

        var url = "https://litteraturbanken.se/api/list_all/etext" +
                  $"?from={startOffset}&to={startOffset + 300}" +
                  "&include=authors,title,titlepath";
        var response = await _httpClient.GetAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        return Content(json, "application/json");
    }

    private static List<LiteraryWork> ParseAndFilter(string json, int fromYear, int toYear)
    {
        var results = new List<LiteraryWork>();

        try
        {
            var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            foreach (var item in data.EnumerateArray())
            {
                var title = item.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString() : null;

                var titlePath = item.TryGetProperty("titlepath", out var pathProp)
                    ? pathProp.GetString() : null;

                string? author = null;
                int? birthYear = null;


                string? authorId = null;
                if (item.TryGetProperty("authors", out var authors) &&
                    authors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in authors.EnumerateArray())
                    {
                        if (a.TryGetProperty("type", out var type) &&
                            type.GetString() == "editor")
                            continue;

                        if (a.TryGetProperty("full_name", out var nameProp))
                            author = nameProp.GetString();

                        // Use the authorid field directly — it's already in the correct format
                        if (a.TryGetProperty("authorid", out var authorIdProp))
                            authorId = authorIdProp.GetString();

                        if (a.TryGetProperty("birth", out var birth) &&
      birth.TryGetProperty("date", out var birthDate))
                        {
                            var dateStr = birthDate.GetString() ?? "";
                            // Handle both "1856" and "1856-03-12" formats
                            var yearStr = dateStr.Length >= 4 ? dateStr[..4] : dateStr;
                            if (int.TryParse(yearStr, out var by))
                                birthYear = by;
                        }

                        break;
                    }

                }


                if (birthYear == null || birthYear < fromYear - 60 || birthYear > toYear)
                    continue;

                results.Add(new LiteraryWork
                {
                    Title = title,
                    Author = author,
                    AuthorBorn = birthYear?.ToString(),
                    Url = (authorId != null && titlePath != null)
                 ? $"https://litteraturbanken.se/f%C3%B6rfattare/{Uri.EscapeDataString(authorId)}/titlar/{Uri.EscapeDataString(titlePath)}/sida/1/etext"
                 : "https://litteraturbanken.se",
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Litteraturbanken response: {ex.Message}");
        }

        return results.Take(5).ToList();
    }
}