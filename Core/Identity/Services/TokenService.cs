using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Text;

using Microsoft.IdentityModel.Tokens;

using UserManagementPoC.Shared.Security.Contracts;

using UserManagementPoC.Shared.Security.Models;

namespace UserManagementPoC.Identity.Services;

public class TokenService : ITokenGenerator, ITokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ClaimsFactory _claimsFactory;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IUserManagementApiClient _userManagementClient;
    public TokenService(IConfiguration configuration, ClaimsFactory claimsFactory, RefreshTokenService refreshTokenService, IUserManagementApiClient userManagementClient)
    {
        _configuration = configuration;
        _claimsFactory = claimsFactory;
        _refreshTokenService = refreshTokenService;
        _userManagementClient = userManagementClient;

    }
    public async Task<TokenResponse> GenerateTokenAsync(UserInfo user, string? securityVersion = null, CancellationToken cancellationToken = default)
    {
        var claims = _claimsFactory.Create(user, securityVersion);
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);
        var refreshToken = await _refreshTokenService.GenerateAsync(user.Id);
        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

    }
    public async Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(secretKey)
            }, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _userManagementClient.GetUserByIdAsync(userId, cancellationToken);

        }
        catch
        {
            return null;

        }
    }
}