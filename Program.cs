using SleepHQImporter.Client;
using Uplink.Applications.Websites.CorporateSites.UplinkBg.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ShortcutUploadStorage>();

// Configure SleepHQ options from configuration
builder.Services.Configure<SleepHQOptions>(
    builder.Configuration.GetSection(SleepHQOptions.SectionName));

// Register token service with its own HttpClient
builder.Services.AddHttpClient<ISleepHQTokenService, SleepHQTokenService>();

// Register auth handler
builder.Services.AddTransient<SleepHQAuthHandler>();

// Register SleepHQ client with auth handler
builder.Services.AddHttpClient<ISleepHQClient, SleepHQClient>(client =>
{
    client.BaseAddress = new Uri("https://sleephq.com/api/");
})
.AddHttpMessageHandler<SleepHQAuthHandler>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
