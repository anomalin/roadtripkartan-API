# RoadtripkartanAPI

ASP.NET Core (.NET 9) Web API that proxies and normalises three public cultural-heritage APIs for the [roadtripkartan](../README.md) client.


## Run

```bash
dotnet run
```


## Wiring

`Program.cs` registers three named `HttpClient`s via `IHttpClientFactory`:

| Registered name | Base address | Timeout | Default headers |
| --- | --- | --- | --- |
| `KSamsok` | *(none)* | 10s | `Accept: application/xml` |
| `MusicBrainz` | `https://musicbrainz.org` | 30s | `Accept: application/json`, `User-Agent: Roadtripkartan/1.0 (portfolio project)` |
| `Litteraturbanken` | `https://litteraturbanken.se` | 15s | `Accept: application/json` |

CORS policy whitelists `http://roadtripkartan.se`.

## Endpoints

All endpoints are unauthenticated and return JSON.

### `GET /api/sites/search`

Search Bebyggelseregistret (`serviceName=bbrb`) on K-Samsök.

**Query params**

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `q` | string | yes | Free-text search. Combined as `text={q} AND serviceName=bbrb`. |

**Response** — `200 OK`, array of:

```json
[
  {
    "id": "http://kulturarvsdata.se/raa/bbr/21400000580062",
    "name": "Nääs slott",
    "description": "...",
    "thumbnail": "https://...",
    "url": "https://kulturarvsdata.se/...",
    "lat": 57.814,
    "lon": 12.394
  }
]
```

`description` is the concatenation of every `itemDescription` field longer than 20 characters (a heuristic to filter out junk fragments like `"02"` or `"Sadeltak"` that K-Samsök sometimes returns alongside the real description). `lat`/`lon` are parsed with invariant culture and may be `null`.

**Errors**

- `400` — empty/missing `q`
- Upstream status — passed through with the response body in the message

### `GET /api/sites/debug`

Returns the raw K-Samsök XML response (2 hits). Intended for development only.

---

### `GET /api/music`

Curated query against MusicBrainz for the supplied year range.

**Query params**

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `fromYear` | int | yes | Start year, > 0. |
| `toYear` | int | yes | End year, ≥ `fromYear`. |

**Behaviour.** The controller does *not* query MusicBrainz by year directly. Instead, it picks an editorial list of artists/composers based on the midpoint of the supplied range (see `GetComposersForPeriod` in `MusicController.cs`) and queries MusicBrainz per artist. Artists are split into two query types:

- `Work` — `/ws/2/work?query=artist:"…"&inc=relations` (classical composers)
- `Recording` — `/ws/2/recording?query=artist:"…"&inc=artist-credits+releases` (recorded music)

The controller stops once it has collected `maxWorks = 10` items.

**Response** — `200 OK`, array of:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "title": "...",
    "composer": "Jean Sibelius",
    "date": "1899",
    "tags": ["symphony", "romantic"]
  }
]
```

For recordings, `composer` holds the artist credit. `date` is `first-release-date` for works; for recordings it falls back to the earliest `releases[].date` (lexicographically — works for ISO dates).

**Errors**

- `400` — invalid date range

### `GET /api/literature`

Pulls a slice of Litteraturbanken's e-text catalog and filters locally by author birth year.

**Query params**

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `fromYear` | int | yes | Start year, > 0. |
| `toYear` | int | yes | End year, ≥ `fromYear`. |

**Behaviour.** Calls `GET /api/list_all/etext?from={offset}&to={offset+300}&include=authors,title,titlepath` against Litteraturbanken with a deterministic offset (seeded on `fromYear + toYear`, then `random.Next(0, 50)` — same range produces the same offset across calls). Filters out editors, keeps the first non-editor author per work, parses the author's birth year, and keeps works where `birthYear ∈ [fromYear - 60, toYear]`. Returns the first 5 matches.

**Response** — `200 OK`, array of:

```json
[
  {
    "id": null,
    "title": "...",
    "author": "Selma Lagerlöf",
    "authorBorn": "1858",
    "url": "https://litteraturbanken.se/författare/.../titlar/.../sida/1/etext"
  }
]
```

`id` is always `null` (the model has the field but the parser does not populate it). `url` falls back to `https://litteraturbanken.se` when `authorid` or `titlepath` is missing.

**Errors**

- `400` — invalid date range
- Upstream status — passed through

### `GET /api/literature/debug`

Returns the raw Litteraturbanken JSON for a random 300-item window. Development only.

## Models

```csharp
// Models/LiteraryWork.cs
public record LiteraryWork
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? AuthorBorn { get; init; }
    public string? Url { get; init; }
}

// Models/MusicWork.cs
public record MusicWork
{
    public string? Id { get; init; }
    public string? Title { get; init; }
    public string? Composer { get; init; }
    public string? Date { get; init; }
    public List<string> Tags { get; init; } = new();
}

// SitesController.cs (record lives next to the controller)
public record SiteResult(
    string? Id, string? Name, string? Description,
    string? Thumbnail, string? Url,
    double? Lat, double? Lon);
```

## Error handling

Errors are deliberately thin: upstream non-2xx statuses are returned with a short message, and JSON/XML parse failures are caught and logged to `Console.WriteLine`. There's no structured logging, no Polly retry/circuit-breaker, and no correlation IDs. If this graduates beyond a portfolio project, that's where I will start working.


