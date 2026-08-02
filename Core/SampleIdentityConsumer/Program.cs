using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Microsoft.IdentityModel.Tokens;

using Scalar.AspNetCore;

using UserManagementPoC.Shared.Authorization.Client;

using UserManagementPoC.Shared.Authorization.Contracts;

using UserManagementPoC.SampleIdentityConsumer.Services;

var builder = WebApplication.CreateBuilder(args);
 builder.AddServiceDefaults();
 var jwtSettings = builder.Configuration.GetSection("JwtSettings");
 var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
 builder.Services.AddAuthentication(options => {
 options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
 options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
 
}).AddJwtBearer(options => {
 options.TokenValidationParameters = new TokenValidationParameters {
 ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = jwtSettings["Issuer"], ValidAudience = jwtSettings["Audience"], IssuerSigningKey = new SymmetricSecurityKey(secretKey) 
};
 
});
 builder.Services.AddIdentityAuthorization(options => {
 options.ServiceName = "identity";
 options.Authority = "https://localhost:7057";
 
});
 builder.Services.AddScoped<IWorkflowContextResolver, SampleWorkflowContextResolver>();
 builder.Services.AddScoped<IResourceScopeResolver, SampleResourceScopeResolver>();
 builder.Services.AddControllers();
 builder.Services.AddOpenApi();
 var app = builder.Build();
 app.MapDefaultEndpoints();
 if (app.Environment.IsDevelopment()) {
 app.MapOpenApi();
 app.MapScalarApiReference();
 
}
app.UseHttpsRedirection();
 app.UseAuthentication();
 app.UseAuthorization();
 app.MapControllers();
 app.Run();
