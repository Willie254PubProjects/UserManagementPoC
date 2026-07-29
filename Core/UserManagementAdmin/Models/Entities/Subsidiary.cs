using System.ComponentModel.DataAnnotations;

using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class Subsidiary : BaseEntityWithExpiry
    {
        [Key] public string Id { get; set; } = KeyGen.GenerateKey();
        public int BankId { get; set; }
        public string Description { get; set; }
        public string CountryCode { get; set; }
        public IEnumerable<Branch> Branches { get; set; }
    }
}