using Microsoft.AspNetCore.Http;

using UserManagementPoC.Shared.Security.Contracts;

using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Identity.Services;

public class AuthenticationService : IUserAuthenticator
{
    private readonly IUserManagementApiClient _userManagementClient;
    private readonly IEncryptionService _encryptionService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthenticationService(IUserManagementApiClient userManagementClient, IEncryptionService encryptionService, ITokenGenerator tokenGenerator, IHttpContextAccessor httpContextAccessor)
    {
        _userManagementClient = userManagementClient;
        _encryptionService = encryptionService;
        _tokenGenerator = tokenGenerator;
        _httpContextAccessor = httpContextAccessor;

    }
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var encryptedPassword = _encryptionService.Encrypt(request.Password, out var iv);
        var verifyResponse = await _userManagementClient.VerifyCredentialsAsync(new VerifyCredentialsRequest
        {
            Username = request.Username,
            EncryptedPassword = encryptedPassword,
            Iv = iv
        }, cancellationToken);

        if (verifyResponse is not
            {
                Success: true, User: not null
            })
        {
            return new LoginResponse {};

        }

        var httpContext = _httpContextAccessor.HttpContext;
        var remoteIp = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].FirstOrDefault();

        var session = await _userManagementClient.CreateSessionAsync(new CreateSessionRequest
        {
            UserId = verifyResponse.User.Id,
            RemoteIp = remoteIp,
            UserAgent = userAgent
        }, cancellationToken);
        
        var tokenResponse = await _tokenGenerator.GenerateTokenAsync(verifyResponse.User, session?.SecurityVersion, cancellationToken);
        
        return new LoginResponse
        {
            TokenType = tokenResponse.TokenType,
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            ExpiresAt = tokenResponse.ExpiresAt,
        };

    }
}