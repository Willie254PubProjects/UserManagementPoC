using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Identity;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class BshUser : IdentityUser, IBaseEntityWithExpiry
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [ForeignKey(nameof(Subsidiary))] public string SubsidiaryId { get; set; }
        public Subsidiary Subsidiary { get; set; }
        [ForeignKey(nameof(Branch))] public string BranchId { get; set; }
        public Branch Branch { get; set; }
        public IEnumerable<UserRole> Roles { get; set; } = new List<UserRole>();

    }
}