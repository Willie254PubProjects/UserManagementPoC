using UserManagementPoC.Shared.Helpers;

using UserManagementPoC.Shared.Models;

using System.ComponentModel.DataAnnotations.Schema;

using System.ComponentModel.DataAnnotations;

namespace UserManagementAdmin.Models.Entities
{
    public class WorkflowAction : BaseEntityWithExpiry
    {
        [Key] public string ActionId { get; set; } = KeyGen.GenerateKey();
        public string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        [ForeignKey(nameof(Workflow))] public string WorkflowId { get; set; }
        public WorkflowType Workflow { get; set; }
    }
}