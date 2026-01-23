using SleepHQImporter.Client;
using Uplink.Applications.Websites.CorporateSites.UplinkBg.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<ShortcutUploadStorage>();

builder.Services.AddHttpClient<ISleepHQClient, SleepHQClient>(client =>
{
    client.BaseAddress = new Uri("https://sleephq.com/api");
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
