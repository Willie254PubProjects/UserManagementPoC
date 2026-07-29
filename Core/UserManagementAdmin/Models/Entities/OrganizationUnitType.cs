using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class OrganizationUnitType : BaseEntityWithExpiry
    {
        public string Id { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; }
    }
}