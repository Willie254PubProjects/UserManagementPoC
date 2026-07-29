using System;

using System.Collections.Generic;

using System.Text;

namespace UserManagementPoC.Shared.Responses
{
    public class ServiceError
    {
        public int ErrorCode { get; set; }
        public string Message { get; set; }
    }
}