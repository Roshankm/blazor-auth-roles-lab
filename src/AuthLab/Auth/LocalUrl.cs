namespace AuthLab.Auth;

/// <summary>Prevents open redirects after login and logout.</summary>
public static class LocalUrl
{
    public static string OrHome(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal)
            ? url
            : "/";
}
