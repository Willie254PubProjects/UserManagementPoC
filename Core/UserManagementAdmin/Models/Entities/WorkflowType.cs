using System.ComponentModel.DataAnnotations;

using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

namespace UserManagementAdmin.Models.Entities
{
    public class WorkflowType : BaseEntityWithExpiry
    {
        [Key] public string WorkflowId { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<WorkflowAction> Actions { get; set; }
    }
}