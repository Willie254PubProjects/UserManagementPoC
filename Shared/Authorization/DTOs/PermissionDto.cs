using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagementPoC.Shared.Authorization.DTOs
{
    public class PermissionDto
    {
        public string Code { get; set; }
        public string Description {  get; set; }
        public string[] Scope {  get; set; }
    }
}
