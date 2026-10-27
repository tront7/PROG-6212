using RaceDay.Web.Models;

namespace RaceDay.Web.Helpers;

/// <summary>Keys used to store the signed-in user's state in the server-side session.</summary>
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    public const string FullName = "FullName";
    public const string ProfilePictureUrl = "ProfilePictureUrl";

    /// <summary>The API's own session cookie value, forwarded on every API call for this user.</summary>
    public const string ApiCookie = "ApiCookie";
}

public static class UserSessionExtensions
{
    public static bool IsAuthenticated(this ISession session) => session.GetInt32(SessionKeys.UserId).HasValue;

    public static string? CurrentRole(this ISession session) => session.GetString(SessionKeys.Role);

    /// <summary>Stores the user's id, role and display details after a successful login.</summary>
    public static void StoreUser(this ISession session, AuthDto user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.FullName, user.FullName);
        SetPicture(session, user.ProfilePictureUrl);
    }

    public static void SetPicture(this ISession session, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) session.Remove(SessionKeys.ProfilePictureUrl);
        else session.SetString(SessionKeys.ProfilePictureUrl, url);
    }
}
