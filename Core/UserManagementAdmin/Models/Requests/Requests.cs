namespace UserManagementAdmin.Models.Requests;

public class CreateUserRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
public class RoleRequest
{
    public string RoleName { get; set; }
}
public class CreateRoleRequest
{
    public string Name { get; set; }
}
public class AssignPermissionRequest
{
    public string PermissionId { get; set; }
}
public class CreateWorkflowRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class CreateWorkflowActionRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}
public class CreatePermissionTypeRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
}