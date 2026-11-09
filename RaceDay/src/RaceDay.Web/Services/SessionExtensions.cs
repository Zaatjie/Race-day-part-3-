using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>Keeps the logged-in user's token, role and name in server-side session.</summary>
public static class SessionExtensions
{
    private const string TokenKey = "rd.token";
    private const string RoleKey = "rd.role";
    private const string NameKey = "rd.name";
    private const string PictureKey = "rd.picture";
    private const string UserIdKey = "rd.userid";

    public static void SignIn(this ISession s, AuthResponse auth)
    {
        s.Clear();
        s.SetString(TokenKey, auth.Token);
        s.SetInt32(UserIdKey, auth.User.UserId);
        s.SetString(RoleKey, auth.User.Role);
        s.SetString(NameKey, auth.User.FullName);
        s.SetString(PictureKey, auth.User.ProfilePictureUrl ?? "");
    }

    public static void UpdateProfile(this ISession s, UserDto user)
    {
        s.SetString(NameKey, user.FullName);
        s.SetString(PictureKey, user.ProfilePictureUrl ?? "");
    }

    public static string? GetToken(this ISession s) => s.GetString(TokenKey);
    public static bool IsSignedIn(this ISession s) => !string.IsNullOrEmpty(s.GetString(TokenKey));
    public static string? GetRole(this ISession s) => s.GetString(RoleKey);
    public static string? GetFullName(this ISession s) => s.GetString(NameKey);
    public static int GetUserId(this ISession s) => s.GetInt32(UserIdKey) ?? 0;

    public static string? GetPictureUrl(this ISession s)
    {
        var p = s.GetString(PictureKey);
        return string.IsNullOrEmpty(p) ? null : p;
    }
}
