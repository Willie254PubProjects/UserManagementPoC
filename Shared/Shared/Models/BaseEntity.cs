using System;

using System.Collections.Generic;

using System.Text;

namespace UserManagementPoC.Shared.Models
{
    public interface IBaseEntity
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
        string CreatedBy { get; set; }
        string LastUpdatedBy { get; set; }
    }
    public interface IBaseEntityWithExpiry : IBaseEntity
    {
        DateTime StartDate { get; set; }
        DateTime? EndDate { get; set; }
    }
    public class BaseEntity : IBaseEntity
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastUpdatedBy { get; set; }
    }
    public class BaseEntityWithExpiry : BaseEntity, IBaseEntityWithExpiry
    {
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}