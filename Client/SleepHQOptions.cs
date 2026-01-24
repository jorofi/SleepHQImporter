namespace SleepHQImporter.Client;

public class SleepHQOptions
{
    public const string SectionName = "SleepHQ";

    public string BaseUrl { get; set; } = "https://sleephq.com";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = "read write delete";
}
