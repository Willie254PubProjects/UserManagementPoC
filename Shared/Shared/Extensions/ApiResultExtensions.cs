using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Shared.Responses;

namespace UserManagementPoC.Shared.Extensions;

public static class ApiResultExtensions
{
    public static OkObjectResult ApiOk(this ControllerBase controller, string? message = null)
    {
        return controller.Ok(ApiResponse.Success(message ?? "Success"));

    }
    public static OkObjectResult ApiOk<T>(this ControllerBase controller, T data, string? message = null)
    {
        return controller.Ok(ApiResponse<T>.Success(message ?? "Success", data));

    }
    public static BadRequestObjectResult ApiBadRequest(this ControllerBase controller, string message, List<ServiceError>? errors = null)
    {
        return controller.BadRequest(ApiResponse.Failure(message, errors));

    }
    public static NotFoundObjectResult ApiNotFound(this ControllerBase controller, string? message = null)
    {
        return controller.NotFound(ApiResponse.Failure(message ?? "Resource not found"));

    }
    public static UnauthorizedObjectResult ApiUnauthorized(this ControllerBase controller, string? message = null)
    {
        return controller.Unauthorized(ApiResponse.Failure(message ?? "Unauthorized"));

    }
}