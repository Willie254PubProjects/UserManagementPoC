namespace UserManagementPoC.Shared.Authorization.Constants;

public static class Permissions
{
    public static class Loan
    {
        public const string Create = "Loan.Create.*";
        public const string View = "Loan.View.*";
        public const string Edit = "Loan.Edit.*";
        public const string Approve = "Loan.Approve.*";
        public const string CreateInvoke = "Loan.Create.Invoke";
    }

    public static class AccountOpening
    {
        public const string Create = "AccountOpening.Create.*";
        public const string View = "AccountOpening.View.*";
        public const string Edit = "AccountOpening.Edit.*";
        public const string Approve = "AccountOpening.Approve.*";
    }

    public static class CustomerOnboarding
    {
        public const string Create = "CustomerOnboarding.Create.*";
        public const string View = "CustomerOnboarding.View.*";
        public const string Edit = "CustomerOnboarding.Edit.*";
        public const string Approve = "CustomerOnboarding.Approve.*";
    }
}