using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserManagementPoC.Shared.Helpers;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class UserPermission : BaseEntityWithExpiry
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
        [ForeignKey(nameof(Permission))] public string PermissionId { get; set; }
        public Permission Permission { get; set; }
        [ForeignKey(nameof(User))] public string UserId { get; set; }
        public BshUser User { get; set; }


        [ForeignKey(nameof(OrganizationUnit))]
        public string ScopeOrganizationUnitId { get; set; }
        public OrganizationUnit OrganizationUnit { get; set; }
        public bool CascadeOrgStructure { get; set; }

    }
}
