using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Slotify.API.Services;

public record SupabaseIdentity(Guid Id, string Email);

public interface ISupabaseIdentityVerifier
{
    Task<SupabaseIdentity?> VerifyAsync(string accessToken, CancellationToken cancellationToken);
}

public sealed class SupabaseIdentityVerifier(HttpClient client, IConfiguration configuration)
    : ISupabaseIdentityVerifier
{
    public async Task<SupabaseIdentity?> VerifyAsync(string accessToken, CancellationToken cancellationToken)
    {
        var key = configuration["Supabase:AnonKey"];
        if (!Uri.TryCreate(configuration["Supabase:Url"], UriKind.Absolute, out var baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttps && !baseUrl.IsLoopback) ||
            string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configura Supabase:Url y Supabase:AnonKey para validar sesiones.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl, "/auth/v1/user"));
        request.Headers.Add("apikey", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or
            HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<SupabaseUser>(cancellationToken);
        return user is { IsAnonymous: false, Id: var id, Email: var email } && id != Guid.Empty &&
            !string.IsNullOrWhiteSpace(email) ? new SupabaseIdentity(id, email) : null;
    }

    private sealed record SupabaseUser(Guid Id, string? Email, [property: JsonPropertyName("is_anonymous")] bool IsAnonymous);
}
