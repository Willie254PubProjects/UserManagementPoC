using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class UserSession : BaseEntity
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();
        [ForeignKey(nameof(User))] 
        public string UserId { get; set; }
        public string? RemoteIP { get; set; } = string.Empty;
        public string? UserAgent { get; set; } = string.Empty;
        public string SecurityVersion { get; set; } = KeyGen.GenerateKey();
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? IdleTimeoutMinutes { get; set; }
        public bool IsActive { get; set; } = true;

        public BshUser User { get; set; }

    }
}