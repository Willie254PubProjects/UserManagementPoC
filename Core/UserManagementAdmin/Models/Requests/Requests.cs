using UserManagementAdmin.Models.Entities;

namespace UserManagementAdmin.Models.Requests;

public class CreateUserRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DomicileUnitId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class LinkExternalLoginRequest
{
    public string LoginProvider { get; set; }
    public string ProviderKey { get; set; }
    public string? ProviderDisplayName { get; set; }
}
public class RoleRequest
{
    public string RoleName { get; set; }
    public string ScopeOrganizationUnitId { get; set; }
    public bool CascadeOrgStructure { get; set; }
}
public class AssignPermissionRequest
{
    public string PermissionId { get; set; }
    public string ScopeOrganizationUnitId { get; set; }
    public bool CascadeOrgStructure { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class AssignRoleToAccessGroupRequest
{
    public string RoleId { get; set; }
}
public class AssignPermissionToAccessGroupRequest
{
    public string PermissionId { get; set; }
}
public class AssignUserToAccessGroupRequest
{
    public string UserId { get; set; }
    public string ScopeOrganizationUnitId { get; set; }
    public bool CascadeOrgStructure { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class AssignAccessGroupRequest
{
    public string AccessGroupId { get; set; }
    public string ScopeOrganizationUnitId { get; set; }
    public bool CascadeOrgStructure { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class CreateRoleRequest
{
    public string Name { get; set; }
}
public class CreatePermissionTypeRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class CreateSubPermissionRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class CreateOrganizationUnitRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string UnitCode { get; set; }
    public string CountryCode { get; set; }
    public string TypeId { get; set; }
    public string? ParentId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class UpdateOrganizationUnitRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string UnitCode { get; set; }
    public string CountryCode { get; set; }
    public string TypeId { get; set; }
    public string? ParentId { get; set; }
    public OrganizationUnitStatus Status { get; set; }
    public DateTime? EndDate { get; set; }
}
public class CreateOrganizationUnitTypeRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsSubsidiary { get; set; }
}
public class CreateAccessGroupRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class UpdateAccessGroupRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime? EndDate { get; set; }
}
public class UpdateUserRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string DomicileUnitId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class UpdateRoleRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class CreatePermissionRequest
{
    public string PermissionTypeId { get; set; }
    public string SubPermissionId { get; set; }
    public string? Description { get; set; }
}
public class UpdatePermissionTypeRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class UpdateSubPermissionRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class UpdateOrganizationUnitTypeRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsSubsidiary { get; set; }
}
public class UpdateAssignmentScopeRequest
{
    public string ScopeOrganizationUnitId { get; set; }
    public bool CascadeOrgStructure { get; set; }
}
