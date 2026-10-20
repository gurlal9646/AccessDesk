namespace AccessDesk.Api.Security;

public static class Roles
{
    public const string Employee = "employee";
    public const string Manager = "manager";
    public const string Admin = "admin";
}

public static class Policies
{
    public const string CanDecide = "CanDecide";
    public const string CanViewAudit = "CanViewAudit";
}
