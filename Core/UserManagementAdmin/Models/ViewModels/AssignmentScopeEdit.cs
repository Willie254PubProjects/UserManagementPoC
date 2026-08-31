namespace UserManagementAdmin.Models.ViewModels;

public class AssignmentScopeEdit
{
    public string AssignmentId { get; set; }
    public string ScopeOrganizationUnitId { get; set; }
    public bool CascadeOrgStructure { get; set; }
    public string UpdateAction { get; set; }
    public string RemoveAction { get; set; }
}