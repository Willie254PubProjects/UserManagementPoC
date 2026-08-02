using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementAdmin.Models.Entities
{
    public class AccessGroupRole
    {
        public string AccessGroupId { get; set; }
        public string RoleId { get; set; }


        [ForeignKey(nameof(AccessGroupId))]
        public AccessGroup AccessGroup { get; set; }

        [ForeignKey(nameof(RoleId))]
        public BshRole Role { get; set; }
    }
}
