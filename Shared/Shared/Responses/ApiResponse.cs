using System;

using System.Collections.Generic;

using System.Text;

using System.Text.Json.Serialization;

namespace UserManagementPoC.Shared.Responses
{
    public class ApiResponse
    {
        public string StatusCode
        {
            get => Status?.Code.ToString() ?? string.Empty;

        }
        public string Description
        {
            get => Status?.Message ?? string.Empty;

        }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
        public List<ServiceError>? Error { get; set; }
        [JsonIgnore] public ResponseStatus Status { get; set; }
        public static ApiResponse Success(string message, object? data = null, ResponseStatus? status = default)
        {
            return new ApiResponse
            {
                IsSuccess = true,
                Data = data,
                Message = message,
                Status = status ?? ResponseStatus.Ok("Request processed successfully!")
            };

        }
        public static ApiResponse Failure(string message, List<ServiceError>? error = null, ResponseStatus? status = default)
        {
            return new ApiResponse
            {
                IsSuccess = false,
                Error = error,
                Message = message,
                Status = status ?? ResponseStatus.BadRequest("Request could not be processed.")
            };

        }
    }
    public class ApiResponse<T> : ApiResponse
    {
        public new T? Data { get; set; }
        public static ApiResponse<T> Success(string message, T? data = default, ResponseStatus? status = default)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message,
                Status = status ?? ResponseStatus.Ok("Request processed successfully!")
            };

        }
        public static ApiResponse<T> Failure(string message, List<ServiceError>? errors = default, ResponseStatus? status = default)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Error = errors,
                Message = message,
                Status = status ?? ResponseStatus.BadRequest("Request could not be processed.")
            };

        }
    }
}