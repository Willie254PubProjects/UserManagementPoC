using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using UserManagementAdmin.Data;
using UserManagementAdmin.Models.Entities;
using UserManagementAdmin.Persistence;
using UserManagementAdmin.Services;
using UserManagementPoC.Shared.Repositories;
using UserManagementPoC.Shared.Security;
using UserManagementPoC.Shared.Security.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDbContext<AdminDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<BshUser, BshRole>().AddEntityFrameworkStores<AdminDbContext>();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddRepositories<AdminDbContext>();
builder.Services.AddScoped<IKeyVaultService, ConfigKeyVaultService>();
builder.Services.AddSharedSecurity();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PermissionAssignmentService>();
builder.Services.AddScoped<WorkflowAdministrationService>();
builder.Services.AddScoped<UserSessionService>();

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
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

app.Run();
