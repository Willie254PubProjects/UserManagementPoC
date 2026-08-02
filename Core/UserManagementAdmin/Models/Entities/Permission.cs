using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class Permission : BaseEntityWithExpiry
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();


        [ForeignKey(nameof(SubPermission))] 
        public string SubPermissionId { get; set; }


        [ForeignKey(nameof(Type))]
        public string PermissionTypeId { get; set; }

        public string Description { get; set; }

        public string Code => $"{Type?.Name ?? throw new Exception("Permission type not defined!.") }.{SubPermission?.Name ?? "*"}";

        public SubPermission SubPermission { get; set; }
        public PermissionType Type { get; set; }

        public IEnumerable<RolePermission> Roles { get; set; }
        public IEnumerable<AccessGroupPermission> AccessGroups { get; set; }
    }
}