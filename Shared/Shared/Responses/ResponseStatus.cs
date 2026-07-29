using System;

using System.Collections.Generic;

using System.Text;

namespace UserManagementPoC.Shared.Responses
{
    public class ResponseStatus
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public static ResponseStatus Ok(string message)
        {
            return new ResponseStatus
            {
                Code = 200,
                Message = message
            };

        }
        public static ResponseStatus BadRequest(string message)
        {
            return new ResponseStatus
            {
                Code = 400,
                Message = message
            };

        }
        public static ResponseStatus Forbidden(string message)
        {
            return new ResponseStatus
            {
                Code = 403,
                Message = message
            };

        }
        public static ResponseStatus UnAuthorized(string message)
        {
            return new ResponseStatus
            {
                Code = 401,
                Message = message
            };

        }
    }
}