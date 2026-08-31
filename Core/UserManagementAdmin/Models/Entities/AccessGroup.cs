using UserManagementPoC.Shared.Helpers;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class AccessGroup : BaseEntityWithExpiry
    {
        public string Id { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; }


        public IEnumerable<AccessGroupPermission> Permissions { get; set; }
        public IEnumerable<AccessGroupRole> Roles { get; set; }
        public IEnumerable<UserAccessGroup> Users { get; set; }
    }
}
