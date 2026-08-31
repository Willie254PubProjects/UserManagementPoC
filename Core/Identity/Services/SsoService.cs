using System.Security.Claims;

using UserManagementPoC.Identity.Models;

using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Identity.Services;

public class SsoService
{
    public const string ExternalLoginProvider = "EntraId";

    private readonly IUserManagementApiClient _userManagementClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    public SsoService(IUserManagementApiClient userManagementClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _userManagementClient = userManagementClient;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public async Task<SsoLoginResult?> CompleteLoginAsync(ClaimsPrincipal? principal, CancellationToken cancellationToken = default)
    {
        if (principal == null) return null;

        var providerKey = principal.FindFirst("oid")?.Value
                          ?? principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value
                    ?? principal.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrEmpty(providerKey) || string.IsNullOrEmpty(email)) return null;

        var user = await _userManagementClient.FindByExternalLoginAsync(ExternalLoginProvider, providerKey, cancellationToken);

        if (user == null)
        {
            user = await _userManagementClient.FindByEmailAsync(email, cancellationToken);
            if (user != null)
            {
                var linked = await _userManagementClient.LinkExternalLoginAsync(user.Id, ExternalLoginProvider, providerKey, "Microsoft Entra ID", cancellationToken);
                if (!linked) return null;
            }
        }

        if (user == null) return null;

        var httpContext = _httpContextAccessor.HttpContext;
        var remoteIp = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault();

        var session = await _userManagementClient.CreateSessionAsync(new CreateSessionRequest
        {
            UserId = user.Id,
            RemoteIp = remoteIp,
            UserAgent = userAgent
        }, cancellationToken);

        if (session == null || string.IsNullOrEmpty(session.SecurityVersion)) return null;

        return new SsoLoginResult
        {
            User = user,
            SecurityVersion = session.SecurityVersion
        };
    }

    public static string? ValidateReturnUrl(string? returnUrl, string? clientId, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return null;
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
        if (!string.IsNullOrEmpty(uri.UserInfo)) return null;

        var isDevelopment =
            string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(configuration["DOTNET_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase);
        if (isDevelopment
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase)))
        {
            return uri.AbsoluteUri;
        }

        var allowedHosts = GetAllowedHosts(clientId, configuration);
        if (allowedHosts.Count == 0) return null;
        if (!allowedHosts.Contains(uri.Authority)) return null;

        return uri.AbsoluteUri;
    }

    public static string ResolveErrorTarget(string? returnUrl, string? clientId, IConfiguration configuration)
    {
        var validated = ValidateReturnUrl(returnUrl, clientId, configuration);
        if (validated != null) return validated;
        return ResolveDefaultReturnUrl(clientId, configuration) ?? "/";
    }

    public static string? ResolveDefaultReturnUrl(string? clientId, IConfiguration configuration)
    {
        if (!string.IsNullOrEmpty(clientId))
        {
            var clients = configuration.GetSection("ApiClients").Get<ApiClientConfig[]>() ?? [];
            var client = clients.FirstOrDefault(c => string.Equals(c.ClientId, clientId, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(client?.DefaultReturnUrl)) return client.DefaultReturnUrl;
        }

        return configuration["OpenIdConnect:DefaultReturnUrl"];
    }

    private static HashSet<string> GetAllowedHosts(string? clientId, IConfiguration configuration)
    {
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(clientId))
        {
            var clients = configuration.GetSection("ApiClients").Get<ApiClientConfig[]>() ?? [];
            var client = clients.FirstOrDefault(c => string.Equals(c.ClientId, clientId, StringComparison.OrdinalIgnoreCase));
            if (client?.AllowedReturnUrlHosts != null)
            {
                foreach (var host in client.AllowedReturnUrlHosts)
                {
                    if (!string.IsNullOrWhiteSpace(host)) hosts.Add(host);
                }
            }
        }

        var global = configuration.GetSection("OpenIdConnect:AllowedReturnUrlHosts").Get<string[]>() ?? [];
        foreach (var host in global)
        {
            if (!string.IsNullOrWhiteSpace(host)) hosts.Add(host);
        }

        return hosts;
    }
}

public class SsoLoginResult
{
    public UserInfo User { get; set; }
    public string SecurityVersion { get; set; }
}