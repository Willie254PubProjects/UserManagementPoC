namespace UserManagementPoC.Shared.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}