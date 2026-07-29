using System.ComponentModel.DataAnnotations;

using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class Permission : BaseEntityWithExpiry
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();
        public string WorkflowId { get; set; }
        public WorkflowType Workflow { get; set; }
        public string? ActionId { get; set; }
        public WorkflowAction? Action { get; set; }
        public string TypeId { get; set; }
        public PermissionType Type { get; set; }
        public string Name => $"{Workflow.Name}.{Action?.Name ?? "*"}.{Type.Name}";
        public IEnumerable<RolePermission> Roles { get; set; }
    }
}