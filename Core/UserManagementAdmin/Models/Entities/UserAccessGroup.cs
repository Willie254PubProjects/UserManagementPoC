using System.ComponentModel.DataAnnotations.Schema;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class UserAccessGroup : BaseEntityWithExpiry
    {
        [ForeignKey(nameof(AccessGroup))] public string AccessGroupId { get; set; }
        public AccessGroup AccessGroup { get; set; }
        [ForeignKey(nameof(User))] public string UserId { get; set; }
        public BshUser User { get; set; }


        [ForeignKey(nameof(OrganizationUnit))]
        public string ScopeOrganizationUnitId { get; set; }
        public OrganizationUnit OrganizationUnit { get; set; }
        public bool CascadeOrgStructure {  get; set; }

    }
}
