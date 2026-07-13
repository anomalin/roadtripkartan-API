using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers();


builder.Services.AddHttpClient("KSamsok", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/xml");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("MusicBrainz", client =>
{
    client.BaseAddress = new Uri("https://musicbrainz.org");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "Locus/1.0 (portfolio project)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Litteraturbanken", client =>
{
    client.BaseAddress = new Uri("https://litteraturbanken.se");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(15);
});


// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173",
        "https://roadtripkartan.se",
        "https://www.roadtripkartan.se")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseRateLimiter();





app.UseHttpsRedirection();

//app.UseCors("ViteDev");

//app.UseAuthorization();
app.MapControllers().RequireRateLimiting("default");


app.Run();


