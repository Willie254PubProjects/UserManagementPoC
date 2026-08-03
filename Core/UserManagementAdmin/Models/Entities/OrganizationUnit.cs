using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

using System.ComponentModel.DataAnnotations.Schema;

namespace UserManagementAdmin.Models.Entities
{
    public class OrganizationUnit : BaseEntityWithExpiry
    {
        public string Id { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; }
        public string UnitCode { get; set; }
        public string CountryCode { get; set; }
        public OrganizationUnitStatus Status { get; set; } = OrganizationUnitStatus.Active;
        [ForeignKey(nameof(Type))] public string TypeId { get; set; }
        public OrganizationUnitType Type { get; set; }
        public string? ParentId { get; set; }
        public OrganizationUnit Parent { get; set; }
        public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();

    }
}