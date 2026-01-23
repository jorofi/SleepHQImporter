using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SleepHQImporter.Client;

public interface ISleepHQTokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

public class SleepHQTokenService : ISleepHQTokenService
{
    private readonly HttpClient _httpClient;
    private readonly SleepHQOptions _options;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public SleepHQTokenService(HttpClient httpClient, IOptions<SleepHQOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // Return cached token if still valid (with 60 second buffer)
        if (_accessToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < _tokenExpiry)
        {
            return _accessToken;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_accessToken is not null && DateTimeOffset.UtcNow.AddSeconds(60) < _tokenExpiry)
            {
                return _accessToken;
            }

            var tokenResponse = await RequestTokenAsync(cancellationToken);
            _accessToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            return _accessToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<SleepHQTokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "password",
                ["scope"] = _options.Scope
            })
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<SleepHQTokenResponse>(content)
            ?? throw new InvalidOperationException("Failed to deserialize token response");

        return tokenResponse;
    }
}
