using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Shared.Responses;

namespace UserManagementAdmin.Extensions;

public static class ApiResultIdentityExtensions
{
    public static BadRequestObjectResult ApiBadRequest(this ControllerBase controller, IdentityResult result, string? message = null)
    {
        var errors = result.Errors.Select(e => new ServiceError
        {
            Message = e.Description
        }).ToList();
        return controller.BadRequest(ApiResponse.Failure(message ?? "Operation failed", errors));

    }
}