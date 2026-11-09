namespace RaceDay.Web.Helpers
{
    /// <summary>Short helpers for reading who is logged in from the web app's session.</summary>
    public static class UserSessionExtensions
    {
        public const string UserIdKey = "UserId";
        public const string RoleKey = "Role";
        public const string EmailKey = "Email";
        public const string DisplayNameKey = "DisplayName";

        public static int? GetUserId(this ISession session) => session.GetInt32(UserIdKey);

        public static string? GetRole(this ISession session) => session.GetString(RoleKey);

        public static bool IsLoggedIn(this ISession session) => session.GetInt32(UserIdKey) is not null;
    }
}
