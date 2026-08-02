using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementAdmin.Models.Entities
{
    public class AccessGroupPermission
    {        
        public string AccessGroupId {  get; set; }
        public string PermissionId {  get; set; }

        [ForeignKey(nameof(AccessGroupId))]
        public AccessGroup AccessGroup { get; set; }

        [ForeignKey(nameof (PermissionId))]
        public Permission Permission { get; set; }
    }
}
