using Microsoft.AspNetCore.Identity;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class BshRole : IdentityRole, IBaseEntityWithExpiry
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public IEnumerable<UserRole> Users { get; set; }
        public IEnumerable<RolePermission> Permissions { get; set; }
    }
}