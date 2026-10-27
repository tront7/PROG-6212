using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using RaceDay.Web.Helpers;

namespace RaceDay.Web.Services;

public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string url);
    Task<ApiResult<T>> PostAsync<T>(string url, object? body = null);
    Task<ApiResult<T>> PutAsync<T>(string url, object? body = null);
    Task<ApiResult<Empty>> DeleteAsync(string url);
    Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file);
}

/// <summary>
/// The MVC app's only route to the data: every call goes to the Part 2 API over HTTP.
/// The API identifies users with its own session cookie. That cookie is kept in this app's
/// per-user session and replayed on every request, so each user acts as themselves.
/// </summary>
public class ApiClient : IApiClient
{
    private const string ApiCookieName = "RaceDay.Session";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<ApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url, null);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Post, url, ToJson(body));

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body = null) =>
        SendAsync<T>(HttpMethod.Put, url, ToJson(body));

    public Task<ApiResult<Empty>> DeleteAsync(string url) => SendAsync<Empty>(HttpMethod.Delete, url, null);

    public Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", file.FileName);

        return SendAsync<T>(HttpMethod.Post, url, form);
    }

    private static HttpContent? ToJson(object? body) =>
        body is null ? null : JsonContent.Create(body, body.GetType(), options: JsonOptions);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        var session = _accessor.HttpContext?.Session;
        var apiCookie = session?.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={apiCookie}");
        }

        try
        {
            using var response = await _http.SendAsync(request);
            CaptureApiCookie(response, session);

            var text = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (typeof(T) == typeof(Empty))
                {
                    data = (T)(object)new Empty();
                }
                else if (!string.IsNullOrWhiteSpace(text))
                {
                    data = JsonSerializer.Deserialize<T>(text, JsonOptions);
                }

                return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
            }

            if ((int)response.StatusCode >= 500)
            {
                _logger.LogError("API {Method} {Url} returned {Status}: {Body}", method, url, (int)response.StatusCode, text);
            }

            return new ApiResult<T>
            {
                Success = false,
                StatusCode = response.StatusCode,
                Error = ExtractError(text, response.StatusCode)
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not reach the API at {Url}", url);
            return Unavailable<T>("We couldn't reach the RaceDay service. Please try again in a moment.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "API request timed out: {Url}", url);
            return Unavailable<T>("The RaceDay service took too long to respond. Please try again.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Unexpected API response for {Url}", url);
            return Unavailable<T>("We received an unexpected response from the RaceDay service.");
        }
    }

    private static ApiResult<T> Unavailable<T>(string message) => new()
    {
        Success = false,
        StatusCode = HttpStatusCode.ServiceUnavailable,
        Error = message
    };

    /// <summary>Remembers (or clears) the API's session cookie whenever the API sets it.</summary>
    private static void CaptureApiCookie(HttpResponseMessage response, ISession? session)
    {
        if (session is null || !response.Headers.TryGetValues("Set-Cookie", out var headers)) return;

        foreach (var header in headers)
        {
            if (!header.StartsWith(ApiCookieName + "=", StringComparison.Ordinal)) continue;

            var value = header[(ApiCookieName.Length + 1)..].Split(';')[0];
            if (string.IsNullOrEmpty(value)) session.Remove(SessionKeys.ApiCookie);
            else session.SetString(SessionKeys.ApiCookie, value);
        }
    }

    /// <summary>Turns an API error body into one friendly sentence. Server errors never leak details.</summary>
    private static string ExtractError(string body, HttpStatusCode status)
    {
        if ((int)status >= 500)
            return "Something went wrong on our side. Please try again.";

        var text = (body ?? string.Empty).Trim();

        if (text.StartsWith('{'))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        if (field.Value.ValueKind != JsonValueKind.Array) continue;
                        foreach (var message in field.Value.EnumerateArray())
                        {
                            var first = message.GetString();
                            if (!string.IsNullOrWhiteSpace(first)) return first;
                        }
                    }
                }

                if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    return detail.GetString() ?? string.Empty;

                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
                // fall through to the defaults below
            }
        }
        else if (text.Length > 0 && text.Length < 300 && !text.StartsWith('<'))
        {
            return text.Trim('"');
        }

        return status switch
        {
            HttpStatusCode.BadRequest => "Some of the information provided isn't valid.",
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing information.",
            _ => "Something went wrong. Please try again."
        };
    }
}
