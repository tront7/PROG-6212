using System.Net;

namespace RaceDay.Web.Services;

/// <summary>Marker type for API calls that return no body.</summary>
public sealed class Empty { }

/// <summary>The outcome of one call to the RaceDay API.</summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }

    /// <summary>A short, user-friendly message when the call failed.</summary>
    public string? Error { get; init; }

    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
}
