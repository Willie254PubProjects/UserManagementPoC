using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using UserManagementPoC.SampleIdentityConsumer.Services;

using UserManagementPoC.Shared.Authorization.Client;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.Shared.Authorization.Sso;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
var identityAuthority = builder.Configuration["IdentityAuthority"] ?? "https://localhost:7057";
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
builder.Services.AddIdentityAuthorization(options =>
{
    options.ServiceName = "identity";
    options.Authority = identityAuthority;

});
builder.Services.AddScoped<IWorkflowContextResolver, SampleWorkflowContextResolver>();
builder.Services.AddScoped<IResourceScopeResolver, SampleResourceScopeResolver>();
builder.Services.AddIdentitySsoClient(identityAuthority);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
var app = builder.Build();
app.MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

}
app.UseHttpsRedirection();
app.UseMiddleware<CookieToBearerMiddleware>();
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "no-cache, max-age=0";
        ctx.Context.Response.Headers.Pragma = "no-cache";
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();