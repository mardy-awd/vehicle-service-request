using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace VehicleService.Web.Services;

public sealed class VehicleServiceApiClient
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VehicleServiceApiClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirstValue("access_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException();
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _client.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data, CancellationToken cancellationToken = default, bool requiresAuthentication = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(data)
        };

        if (requiresAuthentication)
        {
            var token = _httpContextAccessor.HttpContext?
                .User.FindFirstValue("access_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedAccessException();
            }

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await _client.SendAsync(request, cancellationToken);
    }
}