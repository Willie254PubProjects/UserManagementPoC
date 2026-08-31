using Microsoft.AspNetCore.Http;

namespace UserManagementPoC.Shared.Authorization.Sso;

public class CookieToBearerMiddleware
{
    private readonly RequestDelegate _next;
    public CookieToBearerMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var token = context.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }
        }
        await _next(context);
    }
}