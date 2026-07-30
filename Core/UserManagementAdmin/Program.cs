using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using UserManagementAdmin.Data;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Persistence;
using UserManagementAdmin.Services;
using UserManagementAdmin.Services.Interfaces;
using UserManagementPoC.Shared.Repositories;
using UserManagementPoC.Shared.Security;
using UserManagementPoC.Shared.Security.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDbContext<AdminDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<BshUser, BshRole>().AddEntityFrameworkStores<AdminDbContext>();
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
 builder.Services.AddAuthorization();
 builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddRepositories<AdminDbContext>();
builder.Services.AddScoped<IKeyVaultService, ConfigKeyVaultService>();
builder.Services.AddSharedSecurity();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionAssignmentService, PermissionAssignmentService>();
builder.Services.AddScoped<IWorkflowAdministrationService, WorkflowAdministrationService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await context.Database.MigrateAsync();
}

await SeedData.InitializeAsync(app);

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

app.Run();
