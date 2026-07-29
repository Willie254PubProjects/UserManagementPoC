using UserManagementPoC.Shared.Authorization.Models;

namespace UserManagementPoC.Shared.Authorization.Contracts;

public interface IAuthorizationEvaluator
{
    Task<AuthorizationResult> EvaluateAsync(AuthorizationContext context, CancellationToken cancellationToken = default);

}