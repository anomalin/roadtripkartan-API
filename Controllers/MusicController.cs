using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using LocusAPI.Models;

namespace LocusAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MusicController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public MusicController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("MusicBrainz");
    }

    private enum ArtistQueryType
    {
        Work,
        Recording
    }

    private record ArtistQuery(string Name, ArtistQueryType Type);

    [HttpGet]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] int fromYear,
        [FromQuery] int toYear)
    {
        if (fromYear <= 0 || toYear <= 0 || fromYear > toYear)
            return BadRequest("Invalid date range.");

        const int maxWorks = 10;
        var composers = GetComposersForPeriod(fromYear, toYear);
        var allWorks = new List<MusicWork>();

        foreach (var entry in composers)
        {
            if (allWorks.Count >= maxWorks) break;

            var query = $"artist:\"{entry.Name}\"";
            var endpoint = entry.Type == ArtistQueryType.Recording ? "recording" : "work";
            var include = entry.Type == ArtistQueryType.Recording
                ? "&inc=artist-credits+releases"
                : "&inc=relations";

            var url = $"https://musicbrainz.org/ws/2/{endpoint}" +
                      $"?query={Uri.EscapeDataString(query)}" +
                      "&fmt=json" +
                      include +
                      "&limit=2";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) continue;

            var json = await response.Content.ReadAsStringAsync();
            var works = entry.Type == ArtistQueryType.Recording
                ? ParseRecordings(json)
                : ParseWorks(json);
            allWorks.AddRange(works);
        }

        return Ok(allWorks.Take(maxWorks).ToList());
    }

    private static ArtistQuery[] GetComposersForPeriod(int fromYear, int toYear)
    {
        var midpoint = (fromYear + toYear) / 2;

        return midpoint switch
        {
            >= 1960 and <= 1990 => new[]
            {
                new ArtistQuery("Allan Pettersson", ArtistQueryType.Work),
                new ArtistQuery("György Ligeti", ArtistQueryType.Work),
                new ArtistQuery("Krzysztof Penderecki", ArtistQueryType.Work),
                new ArtistQuery("The Beatles", ArtistQueryType.Recording),
                new ArtistQuery("Bob Dylan", ArtistQueryType.Recording),
                new ArtistQuery("Joni Mitchell", ArtistQueryType.Recording),
                new ArtistQuery("David Bowie", ArtistQueryType.Recording),
                new ArtistQuery("Aretha Franklin", ArtistQueryType.Recording),
                new ArtistQuery("Stevie Wonder", ArtistQueryType.Recording),
                new ArtistQuery("Miles Davis", ArtistQueryType.Recording),
                new ArtistQuery("ABBA", ArtistQueryType.Recording),
                new ArtistQuery("Kraftwerk", ArtistQueryType.Recording),
                new ArtistQuery("Cornelis Vreeswijk", ArtistQueryType.Recording),
            },
            >= 1940 and < 1960 => new[]
            {
                new ArtistQuery("Lars-Erik Larsson", ArtistQueryType.Work),
                new ArtistQuery("Allan Pettersson", ArtistQueryType.Work),
                new ArtistQuery("Dmitri Shostakovich", ArtistQueryType.Work),
                new ArtistQuery("Benjamin Britten", ArtistQueryType.Work),
                new ArtistQuery("Frank Sinatra", ArtistQueryType.Recording),
                new ArtistQuery("Billie Holiday", ArtistQueryType.Recording),
                new ArtistQuery("Ella Fitzgerald", ArtistQueryType.Recording),
                new ArtistQuery("Charlie Parker", ArtistQueryType.Recording),
                new ArtistQuery("Miles Davis", ArtistQueryType.Recording),
                new ArtistQuery("Elvis Presley", ArtistQueryType.Recording),
                new ArtistQuery("Hank Williams", ArtistQueryType.Recording),
                new ArtistQuery("Chuck Berry", ArtistQueryType.Recording),
            },
            >= 1920 and < 1940 => new[]
            {
                new ArtistQuery("Igor Stravinsky", ArtistQueryType.Work),
                new ArtistQuery("Béla Bartók", ArtistQueryType.Work),
                new ArtistQuery("George Gershwin", ArtistQueryType.Work),
                new ArtistQuery("Hugo Alfvén", ArtistQueryType.Work),
                new ArtistQuery("Maurice Ravel", ArtistQueryType.Work),
                new ArtistQuery("Evert Taube", ArtistQueryType.Recording),
                new ArtistQuery("Louis Armstrong", ArtistQueryType.Recording),
                new ArtistQuery("Duke Ellington", ArtistQueryType.Recording),
                new ArtistQuery("Bessie Smith", ArtistQueryType.Recording),
                new ArtistQuery("Cab Calloway", ArtistQueryType.Recording),
                new ArtistQuery("Billie Holiday", ArtistQueryType.Recording),
            },
            >= 1850 and < 1920 => new[]
            {
                new ArtistQuery("Wilhelm Stenhammar", ArtistQueryType.Work),
                new ArtistQuery("Hugo Alfvén", ArtistQueryType.Work),
                new ArtistQuery("August Söderman", ArtistQueryType.Work),
                new ArtistQuery("Wilhelm Peterson-Berger", ArtistQueryType.Work),
                new ArtistQuery("Johannes Brahms", ArtistQueryType.Work),
                new ArtistQuery("Edvard Grieg", ArtistQueryType.Work),
                new ArtistQuery("Jean Sibelius", ArtistQueryType.Work),
                new ArtistQuery("Carl Nielsen", ArtistQueryType.Work),
                new ArtistQuery("Giacomo Puccini", ArtistQueryType.Work)
            },
            >= 1780 and < 1850 => new[]
            {
                new ArtistQuery("Mozart", ArtistQueryType.Work),
                new ArtistQuery("Haydn", ArtistQueryType.Work),
                new ArtistQuery("Beethoven", ArtistQueryType.Work),
                new ArtistQuery("Crusell", ArtistQueryType.Work),
                new ArtistQuery("Schubert", ArtistQueryType.Work),
                new ArtistQuery("Chopin", ArtistQueryType.Work),
                new ArtistQuery("Mendelssohn", ArtistQueryType.Work),
            },
            >= 1600 and < 1780 => new[]
            {
                new ArtistQuery("Bach", ArtistQueryType.Work),
                new ArtistQuery("Handel", ArtistQueryType.Work),
                new ArtistQuery("Buxtehude", ArtistQueryType.Work),
                new ArtistQuery("Düben", ArtistQueryType.Work),
                new ArtistQuery("Vivaldi", ArtistQueryType.Work),
                new ArtistQuery("Telemann", ArtistQueryType.Work),
                new ArtistQuery("Roman", ArtistQueryType.Work),
            },
            _ => new[]
            {
                new ArtistQuery("Beethoven", ArtistQueryType.Work),
                new ArtistQuery("Mozart", ArtistQueryType.Work),
                new ArtistQuery("Bach", ArtistQueryType.Work),
            }
        };
    }

    private static List<MusicWork> ParseWorks(string json)
    {
        var results = new List<MusicWork>();

        try
        {
            var doc = JsonDocument.Parse(json);
            var works = doc.RootElement.GetProperty("works");

            foreach (var work in works.EnumerateArray())
            {
                var id = work.TryGetProperty("id", out var idProp)
                    ? idProp.GetString() : null;

                var title = work.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString() : null;

                var date = work.TryGetProperty("first-release-date", out var dateProp)
                    ? dateProp.GetString() : null;

                var tags = new List<string>();
                if (work.TryGetProperty("tags", out var tagsProp))
                {
                    foreach (var tag in tagsProp.EnumerateArray())
                    {
                        if (tag.TryGetProperty("name", out var tagName))
                            tags.Add(tagName.GetString() ?? "");
                    }
                }

                string? composer = null;
                if (work.TryGetProperty("relations", out var relations))
                {
                    foreach (var rel in relations.EnumerateArray())
                    {
                        if (rel.TryGetProperty("type", out var relType) &&
                            relType.GetString() == "composer" &&
                            rel.TryGetProperty("artist", out var artist) &&
                            artist.TryGetProperty("name", out var artistName))
                        {
                            composer = artistName.GetString();
                            break;
                        }
                    }
                }

                results.Add(new MusicWork
                {
                    Id = id,
                    Title = title,
                    Composer = composer,
                    Date = date,
                    Tags = tags,
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing MusicBrainz response: {ex.Message}");
        }

        return results;
    }

    private static List<MusicWork> ParseRecordings(string json)
    {
        var results = new List<MusicWork>();

        try
        {
            var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("recordings", out var recordings))
                return results;

            foreach (var rec in recordings.EnumerateArray())
            {
                var id = rec.TryGetProperty("id", out var idProp)
                    ? idProp.GetString() : null;

                var title = rec.TryGetProperty("title", out var titleProp)
                    ? titleProp.GetString() : null;

                string? artist = null;
                if (rec.TryGetProperty("artist-credit", out var creditArr))
                {
                    foreach (var credit in creditArr.EnumerateArray())
                    {
                        if (credit.TryGetProperty("name", out var nameProp))
                        {
                            artist = nameProp.GetString();
                            break;
                        }
                        if (credit.TryGetProperty("artist", out var artistObj) &&
                            artistObj.TryGetProperty("name", out var artistNameProp))
                        {
                            artist = artistNameProp.GetString();
                            break;
                        }
                    }
                }

                string? date = null;
                if (rec.TryGetProperty("first-release-date", out var dateProp))
                {
                    date = dateProp.GetString();
                }
                else if (rec.TryGetProperty("releases", out var releases))
                {
                    foreach (var rel in releases.EnumerateArray())
                    {
                        if (rel.TryGetProperty("date", out var relDate))
                        {
                            var candidate = relDate.GetString();
                            if (!string.IsNullOrEmpty(candidate) &&
                                (date is null || string.Compare(candidate, date, StringComparison.Ordinal) < 0))
                            {
                                date = candidate;
                            }
                        }
                    }
                }

                var tags = new List<string>();
                if (rec.TryGetProperty("tags", out var tagsProp))
                {
                    foreach (var tag in tagsProp.EnumerateArray())
                    {
                        if (tag.TryGetProperty("name", out var tagName))
                            tags.Add(tagName.GetString() ?? "");
                    }
                }

                results.Add(new MusicWork
                {
                    Id = id,
                    Title = title,
                    Composer = artist,
                    Date = date,
                    Tags = tags,
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing MusicBrainz recordings response: {ex.Message}");
        }

        return results;
    }
}