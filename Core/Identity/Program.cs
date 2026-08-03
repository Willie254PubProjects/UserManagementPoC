using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserManagementPoC.Identity.Services;
using UserManagementPoC.Shared.Abstractions;
using UserManagementPoC.Shared.Security;
using UserManagementPoC.Shared.Security.Contracts;
using UserManagementPoC.Shared.Authorization.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };

});
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 10;
        o.QueueLimit = 0;
    });
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many attempts. Please try again later." }, token);
    };
});
builder.Services.AddScoped<IKeyVaultService, ConfigKeyVaultService>();
builder.Services.AddSharedSecurity();
builder.Services.AddScoped<ITokenGenerator, TokenService>();
builder.Services.AddScoped<ITokenValidator, TokenService>();
builder.Services.AddScoped<IUserAuthenticator, AuthenticationService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<RefreshTokenService>();
builder.Services.AddScoped<ClaimsFactory>();
builder.Services.AddTransient<UserManagementTokenHandler>();

builder.Services.AddHttpClient<UserManagementApiClient>(client =>
    {
        client.BaseAddress = new Uri("https://localhost:7137");

    }).AddHttpMessageHandler<UserManagementTokenHandler>()
    .AddStandardResilienceHandler();

builder.Services.AddScoped<IUserManagementApiClient>(sp => sp.GetRequiredService<UserManagementApiClient>());
builder.Services.AddScoped<IAuthorizationEvaluator, AuthorizationService>();

var app = builder.Build();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

}
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.Run();
