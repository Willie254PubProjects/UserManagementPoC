namespace UserManagementPoC.Shared.Authorization.Constants;

public static class Permissions
{
    public static class CardPrinting
    {
        public const string Create = "CardPrinting.Create";
        public const string View = "CardPrinting.View";
        public const string Edit = "CardPrinting.Edit";
        public const string Approve = "CardPrinting.Approve";
        public const string Submit = "CardPrinting.Submit";
        public const string Invoke = "CardPrinting.Invoke";
    }

    public static class Account
    {
        public const string Create = "Account.Create";
        public const string View = "Account.View";
        public const string Edit = "Account.Edit";
        public const string Approve = "Account.Approve";
        public const string Submit = "Account.Submit";
        public const string Invoke = "Account.Invoke";
    }

    public static class CardRequest
    {
        public const string Create = "CardRequest.Create";
        public const string View = "CardRequest.View";
        public const string Edit = "CardRequest.Edit";
        public const string Approve = "CardRequest.Approve";
        public const string Submit = "CardRequest.Submit";
        public const string Invoke = "CardRequest.Invoke";
    }
}
