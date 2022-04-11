using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace MultipleAuthenticationSchemes;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string _validUserName = "admin";
    private const string _validPassword = "password";

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(HeaderNames.Authorization))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header is not found or empty"));
        }

        var validHeaderValue = AuthenticationHeaderValue.TryParse(Request.Headers[HeaderNames.Authorization],
            out AuthenticationHeaderValue authHeader);
        if (!validHeaderValue || !authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authHeader.Parameter))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header value is not valid"));
        }

        BasicCredentials credentials = ExtractCredentials(authHeader.Parameter);
        if (credentials is null || !credentials.IsValid(_validUserName, _validPassword))
        {
            return Task.FromResult(AuthenticateResult.Fail("Credentials are not valid"));
        }

        AuthenticationTicket ticket = GetAuthenticationTicket(_validUserName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private AuthenticationTicket GetAuthenticationTicket(string username)
    {
        Claim[] claims = { new Claim(ClaimTypes.Name, username) };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return ticket;
    }

    private static BasicCredentials ExtractCredentials(string authorizationParameter)
    {
        byte[] credentialBytes;

        try
        {
            credentialBytes = Convert.FromBase64String(authorizationParameter);
        }
        catch (FormatException)
        {
            return null;
        }

        var decodedCredentials = Encoding.UTF8.GetString(credentialBytes);

        if (string.IsNullOrEmpty(decodedCredentials) || !decodedCredentials.Contains(':'))
        {
            return null;
        }

        var credentials = decodedCredentials.Split(":");
        return credentials.Length < 2
            ? null
            : new BasicCredentials(credentials[0], credentials[1]);
    }

    private record BasicCredentials(string UserName, string Password)
    {
        internal bool IsValid(string validUserName, string validPassword) =>
            UserName == validUserName && Password == validPassword;
    }
}