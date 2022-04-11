using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MultipleAuthenticationSchemes;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string _validApiKey = "KEY";
    private const string _headerName = "X-API-KEY";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(_headerName))
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header is not found or empty"));
        }

        var headerValue = Request.Headers[_headerName];
        if (string.IsNullOrEmpty(headerValue) || headerValue != _validApiKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("Authorization header value is not valid"));
        }

        AuthenticationTicket ticket = GetAuthenticationTicket();
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private AuthenticationTicket GetAuthenticationTicket()
    {
        Claim[] claims = {  };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return ticket;
    }
}