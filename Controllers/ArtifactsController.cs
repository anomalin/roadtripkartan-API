// Controllers/ArtifactsController.cs

using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using LocusAPI.Models;

namespace LocusAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtifactsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public ArtifactsController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("Europeana");
        _apiKey = config["Europeana:ApiKey"]
            ?? throw new InvalidOperationException("Europeana API key not configured.");
    }

    [HttpGet]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] int fromYear,
        [FromQuery] int toYear,
        [FromQuery] string category = "furniture")
    {
        if (fromYear <= 0 || toYear <= 0 || fromYear > toYear)
            return BadRequest("Invalid date range.");

        var categoryQuery = GetCategoryQuery(category);

        var url = "https://api.europeana.eu/record/v2/search.json" +
                  $"?query={Uri.EscapeDataString(categoryQuery)}" +
                  $"&qf=YEAR%3A%5B{fromYear}+TO+{toYear}%5D" +
                  "&qf=TYPE%3AIMAGE" +
                  "&qf=MEDIA%3Atrue" +
                  "&qf=COUNTRY%3Asweden" +
                  "&qf=-what:photograph" +
                  "&qf=-what:fotografi" +
                  "&qf=-what:photo" +
                  "&profile=rich" +
                  "&rows=6" +
                  $"&wskey={_apiKey}";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Error contacting Europeana.");

        var json = await response.Content.ReadAsStringAsync();
        var results = ParseArtifacts(json);

        return Ok(results);
    }

    private static string GetCategoryQuery(string category) => category switch
    {
        "furniture" => "möbel OR furniture OR stol OR bord OR skåp",
        "art" => "painting OR portrait OR tavla OR porträtt",
        "ceramics" => "ceramics OR porcelain OR porslin OR keramik",
        "textiles" => "textile OR fabric OR tapestry OR textil OR vävnad",
        "silver" => "silver OR silversmith OR guldsmed",
        _ => "furniture"
    };

    private static List<Artifact> ParseArtifacts(string json)
    {
        var results = new List<Artifact>();

        try
        {
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("items", out var items))
                return results;

            foreach (var item in items.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idProp)
                    ? idProp.GetString() : null;

                // Title
                string? title = null;
                if (item.TryGetProperty("title", out var titleArr) &&
                    titleArr.ValueKind == JsonValueKind.Array)
                    title = titleArr.EnumerateArray().FirstOrDefault().GetString();

                // Description
                string? description = null;
                if (item.TryGetProperty("dcDescription", out var descArr) &&
                    descArr.ValueKind == JsonValueKind.Array)
                    description = descArr.EnumerateArray().FirstOrDefault().GetString();

                // Thumbnail
                string? thumbnail = null;
                if (item.TryGetProperty("edmPreview", out var thumbArr) &&
                    thumbArr.ValueKind == JsonValueKind.Array)
                    thumbnail = thumbArr.EnumerateArray().FirstOrDefault().GetString();

                // Source URL
                string? sourceUrl = null;
                if (item.TryGetProperty("guid", out var guidProp))
                    sourceUrl = guidProp.GetString();

                // Institution
                string? institution = null;
                if (item.TryGetProperty("dataProvider", out var providerArr) &&
                    providerArr.ValueKind == JsonValueKind.Array)
                    institution = providerArr.EnumerateArray().FirstOrDefault().GetString();

                // Year
                string? year = null;
                if (item.TryGetProperty("year", out var yearArr) &&
                    yearArr.ValueKind == JsonValueKind.Array)
                    year = yearArr.EnumerateArray().FirstOrDefault().GetString();

                // Only include items with thumbnails
                if (thumbnail == null) continue;

                results.Add(new Artifact
                {
                    Id = id,
                    Title = title,
                    Description = description,
                    ThumbnailUrl = thumbnail,
                    SourceUrl = sourceUrl,
                    Institution = institution,
                    Year = year,
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Europeana response: {ex.Message}");
        }

        return results;
    }
}