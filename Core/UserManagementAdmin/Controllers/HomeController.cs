using System.Diagnostics;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementAdmin.Models;

using UserManagementAdmin.Services.Interfaces;

namespace UserManagementAdmin.Controllers;

public class HomeController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IAccessGroupService _accessGroupService;
    private readonly IPermissionAdministrationService _permissionService;
    public HomeController(IUserService userService, IRoleService roleService, IAccessGroupService accessGroupService, IPermissionAdministrationService permissionService)
    {
        _userService = userService;
        _roleService = roleService;
        _accessGroupService = accessGroupService;
        _permissionService = permissionService;
    }
    [Authorize]
    public async Task<IActionResult> Index()
    {
        ViewBag.UserCount = (await _userService.GetAllAsync(1, 1)).TotalCount;
        ViewBag.RoleCount = (await _roleService.GetAllAsync(1, 1)).TotalCount;
        ViewBag.GroupCount = (await _accessGroupService.GetAllAsync(1, 1)).TotalCount;
        ViewBag.PermissionCount = (await _permissionService.GetPermissionsAsync()).Count;
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}