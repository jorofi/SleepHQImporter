using System.Net.Http.Headers;

namespace SleepHQImporter.Client;

public class SleepHQAuthHandler : DelegatingHandler
{
    private readonly ISleepHQTokenService _tokenService;

    public SleepHQAuthHandler(ISleepHQTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
