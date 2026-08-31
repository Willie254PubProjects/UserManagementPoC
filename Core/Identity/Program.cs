using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
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
var oidcSettings = builder.Configuration.GetSection("OpenIdConnect");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

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

}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = oidcSettings["Authority"];
    options.ClientId = oidcSettings["ClientId"];
    options.ClientSecret = oidcSettings["ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.SaveTokens = true;
    options.MapInboundClaims = false;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.Events = new OpenIdConnectEvents
    {
        OnTicketReceived = async context =>
        {
            var ssoService = context.HttpContext.RequestServices.GetRequiredService<SsoService>();
            var codeService = context.HttpContext.RequestServices.GetRequiredService<AuthorizationCodeService>();

            string? clientId = null;
            if (context.Properties?.Items.TryGetValue("client_id", out var cid) == true)
            {
                clientId = cid;
            }
            var returnUrl = context.Properties?.RedirectUri;
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = SsoService.ResolveDefaultReturnUrl(clientId, builder.Configuration);
            }

            var loginResult = await ssoService.CompleteLoginAsync(context.Principal);

            context.HandleResponse();

            var validated = SsoService.ValidateReturnUrl(returnUrl, clientId, builder.Configuration);
            if (loginResult == null || validated == null)
            {
                var reason = loginResult == null ? "no_matching_user" : "return_url_not_permitted";
                var errorTarget = SsoService.ResolveErrorTarget(returnUrl, clientId, builder.Configuration);
                var errorSeparator = errorTarget.Contains('?') ? '&' : '?';
                context.Response.Redirect($"{errorTarget}{errorSeparator}error=access_denied&reason={reason}");
                return;
            }

            var code = await codeService.GenerateAsync(loginResult.User.Id, loginResult.SecurityVersion, clientId ?? "identity");
            var separator = validated.Contains('?') ? '&' : '?';
            context.Response.Redirect($"{validated}{separator}code={Uri.EscapeDataString(code)}");
        }
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
builder.Services.AddScoped<SsoService>();
builder.Services.AddSingleton<AuthorizationCodeService>();
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