using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class UserRole : BaseEntity
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();
        [ForeignKey(nameof(Role))] public string RoleId { get; set; }
        public BshRole Role { get; set; }
        [ForeignKey(nameof(User))] public string UserId { get; set; }
        public BshUser User { get; set; }
    }
}