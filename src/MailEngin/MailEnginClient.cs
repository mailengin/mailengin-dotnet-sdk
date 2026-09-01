using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MailEngin;

public sealed class MailEnginClient : IDisposable
{
    public const string Version = "0.1.0";
    public const string DefaultBaseUrl = "https://api.mailengin.app";
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _timeout;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MailEnginClient(
        string apiKey,
        string baseUrl = DefaultBaseUrl,
        TimeSpan? timeout = null,
        HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("MailEngin requires a non-empty API key.", nameof(apiKey));
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
        if (_timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout), "MailEngin timeout must be positive.");
        _apiKey = apiKey.Trim();
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
        Emails = new EmailsResource(this);
    }

    public EmailsResource Emails { get; }

    internal async Task<JsonDocument> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.UserAgent.ParseAdd("mailengin-dotnet/" + Version);
        request.Content = new StringContent(JsonSerializer.Serialize(body, _jsonOptions), Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MailEnginException("MailEngin request timed out.", errorCode: "request_timeout", innerException: error);
        }
        catch (OperationCanceledException error)
        {
            throw new MailEnginException("MailEngin request was canceled.", errorCode: "request_aborted", innerException: error);
        }
        catch (HttpRequestException error)
        {
            throw new MailEnginException("Unable to reach the MailEngin API.", errorCode: "network_error", innerException: error);
        }

        using (response)
        {
            var raw = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JsonDocument? parsed = null;
            try { if (!string.IsNullOrWhiteSpace(raw)) parsed = JsonDocument.Parse(raw); } catch (JsonException) { }
            if (!response.IsSuccessStatusCode)
            {
                string? code = null;
                var message = $"MailEngin API request failed with status {(int)response.StatusCode}.";
                if (parsed?.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (parsed.RootElement.TryGetProperty("message", out var value) && value.ValueKind == JsonValueKind.String) message = value.GetString()!;
                    if (parsed.RootElement.TryGetProperty("code", out value) && value.ValueKind == JsonValueKind.String) code = value.GetString();
                }
                double? retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds;
                if (!retryAfter.HasValue && double.TryParse(response.Headers.RetryAfter?.ToString(), out var seconds)) retryAfter = seconds;
                var requestId = response.Headers.TryGetValues("x-request-id", out var values) ? values.FirstOrDefault() : null;
                parsed?.Dispose();
                throw new MailEnginException(message, response.StatusCode, code, requestId, retryAfter, raw);
            }
            if (parsed is null || parsed.RootElement.ValueKind != JsonValueKind.Object)
            {
                parsed?.Dispose();
                throw new MailEnginException("MailEngin API returned invalid JSON.", errorCode: "invalid_response", body: raw);
            }
            return parsed;
        }
    }

    public void Dispose()
    {
        if (_ownsClient) _httpClient.Dispose();
    }
}
