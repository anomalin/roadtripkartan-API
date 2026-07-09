using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Xml.Linq;

namespace LocusAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SitesController : ControllerBase
{
    private readonly HttpClient _httpClient;

    public SitesController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("KSamsok");
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var query = Uri.EscapeDataString($"text={q} AND serviceName=bbrb");

        var url = "https://kulturarvsdata.se/ksamsok/api" +
                  "?method=search" +
                  $"&query={query}" +
                  "&recordSchema=xml" +
                  "&fields=itemLabel,itemDescription,thumbnail,url,lat,lon" +
                  "&hitsPerPage=10";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, $"Error contacting K-samsök, returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var xml = await response.Content.ReadAsStringAsync();
        var results = ParseResults(xml);
        return Ok(results);
    }

    [HttpGet("by-id")]
    public async Task<IActionResult> GetById([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("Query parameter 'id' is required.");
        
        var query = Uri.EscapeDataString($"itemId=\"{id}\"");

        var url = "https://kulturarvsdata.se/ksamsok/api" + 
                    "?method=search" +
                    $"&query={query}" + 
                    "&recordSchema=xml" + 
                    "&fields=itemId,itemLabel,itemDescription,thumbnail,url,lat,lon" +
                    "&hitsPerPage=1";
        
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode(
                (int)response.StatusCode,
                $"Error contacting K-samsök: {await response.Content.ReadAsStringAsync()}"
            );
        
        var xml = await response.Content.ReadAsStringAsync();

        var results = ParseResults(xml);

        var site = results.FirstOrDefault();

        if (site is null)
            return NotFound();
        
        return Ok(site);
    }

    [HttpGet("debug")]
    public async Task<IActionResult> Debug([FromQuery] string q)
    {
        var query2 = Uri.EscapeDataString($"text={q} AND (serviceName=bbra OR serviceName=bbrb)");

        var query = Uri.EscapeDataString(
    "itemId=\"http://kulturarvsdata.se/raa/bbr/21400000583230\""
);

        var url = "https://kulturarvsdata.se/ksamsok/api" +
                  "?method=search" +
                  $"&query={query}" +
                  "&recordSchema=xml" +
                  "&fields=itemLabel,itemDescription,thumbnail,url,lat,lon" +
                  "&hitsPerPage=2";

        var response = await _httpClient.GetAsync(url);
        var xml = await response.Content.ReadAsStringAsync();

        return Content(xml, "application/xml");
    }

    private static List<SiteResult> ParseResults(string xml)
    {
        var doc = XDocument.Parse(xml);

        return doc.Descendants("record").Select(r =>
        {
            var fields = r.Elements("field");

            string? Get(string name) =>
                fields.FirstOrDefault(f => f.Attribute("name")?.Value == name)?.Value;

            string? GetDescription(string name) =>
                string.Join(" ", fields
                    .Where(f => f.Attribute("name")?.Value == name)
                    .Select(f => f.Value)
                    .Where(v => v.Length > 20)); // filter out short junk values like "02" or "Sadeltak"

            return new SiteResult
            {
                Id = Get("itemId"),
                Name = Get("itemLabel"),
                Description = GetDescription("itemDescription"),
                Thumbnail = Get("thumbnail"),
                Url = Get("url"),
                Lat = double.TryParse(Get("lat"),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var lat) ? lat : null,
                Lon = double.TryParse(Get("lon"),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out var lon) ? lon : null,
            };
        }).ToList();
    }
}

public record SiteResult(
    string? Id,
    string? Name,
    string? Description,
    string? Thumbnail,
    string? Url,
    double? Lat,
    double? Lon
)
{
    public SiteResult() : this(null, null, null, null, null, null, null) { }
}