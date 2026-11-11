using System.Net;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace AuthLab.Tests;

/// <summary>
/// End-to-end through the real pipeline: post the static SSR login form (with its antiforgery
/// token), keep the auth cookie, then request protected pages as each demo user.
/// </summary>
public partial class SignInFlowTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Anonymous_users_are_sent_to_the_login_page()
    {
        var response = await NewClient().GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Wrong_password_shows_an_error_and_sets_no_cookie()
    {
        var client = NewClient();
        var response = await SignInAsync(client, "alice", "wrong-password");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid user name or password.", await response.Content.ReadAsStringAsync());
        Assert.False(response.Headers.Contains("Set-Cookie") && response.Headers.GetValues("Set-Cookie").Any(c => c.StartsWith("authlab=", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("alice", "/admin", HttpStatusCode.OK)]
    [InlineData("bob", "/admin", HttpStatusCode.Redirect)]       // manager, not admin
    [InlineData("bob", "/approvals", HttpStatusCode.OK)]         // role policy
    [InlineData("carol", "/approvals", HttpStatusCode.Redirect)]
    [InlineData("carol", "/finance", HttpStatusCode.OK)]         // claim policy
    [InlineData("alice", "/finance", HttpStatusCode.Redirect)]   // admin, but not in Finance
    [InlineData("bob", "/senior", HttpStatusCode.OK)]            // custom tenure requirement
    [InlineData("carol", "/senior", HttpStatusCode.Redirect)]
    public async Task Pages_enforce_their_policies(string user, string url, HttpStatusCode expected)
    {
        var client = NewClient();
        var login = await SignInAsync(client, user, "Passw0rd!");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var response = await client.GetAsync(url);

        Assert.Equal(expected, response.StatusCode);
        if (expected == HttpStatusCode.Redirect)
        {
            Assert.Contains("/access-denied", response.Headers.Location!.OriginalString);
        }
    }

    [Fact]
    public async Task Edit_buttons_follow_resource_based_rules()
    {
        var client = NewClient();
        await SignInAsync(client, "carol", "Passw0rd!");

        var html = await client.GetStringAsync("/leave");

        // Carol owns requests 1 (pending) and 3 (approved); request 2 belongs to Bob.
        Assert.Contains("data-test=\"edit\"", Row(html, 1));
        Assert.DoesNotContain("data-test=\"edit\"", Row(html, 2));
        Assert.DoesNotContain("data-test=\"edit\"", Row(html, 3));
    }

    private HttpClient NewClient() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<HttpResponseMessage> SignInAsync(HttpClient client, string user, string password)
    {
        var page = await client.GetStringAsync("/login");
        var token = TokenPattern().Match(page).Groups[1].Value;

        return await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["_handler"] = "login",
            ["Input.UserName"] = user,
            ["Input.Password"] = password,
        }));
    }

    private static string Row(string html, int id)
    {
        var start = html.IndexOf($"data-test=\"request-{id}\"", StringComparison.Ordinal);
        var end = html.IndexOf("</tr>", start, StringComparison.Ordinal);
        return html[start..end];
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
