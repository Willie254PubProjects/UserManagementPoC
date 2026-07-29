using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class OrganizationUnit : BaseEntityWithExpiry
    {
        public string Id { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; }
        public OrganizationUnitType Type { get; set; }
        public string? ParentId { get; set; }
        public OrganizationUnit Parent { get; set; }
        public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();

    }
}