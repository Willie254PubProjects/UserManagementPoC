using System.ComponentModel.DataAnnotations;
using UserManagementPoC.Shared.Helpers;
using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class SubPermission : BaseEntityWithExpiry
    {
        [Key]
        public string Id { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; } // View | Approve | Submit | Invoke etc.
        public string Description { get; set; }
    }
}