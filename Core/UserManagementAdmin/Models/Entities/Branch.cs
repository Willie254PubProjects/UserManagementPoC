using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using UserManagementPoC.Shared.Helpers;

namespace UserManagementAdmin.Models.Entities
{
    public class Branch
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; }
        public string BranchCode { get; set; }
        [ForeignKey(nameof(Subsidiary))] public string SubsidiaryId { get; set; }
        public Subsidiary Subsidiary { get; set; }
    }
}