namespace FormCMS.Auth.Services;

public static class AccessScope
{
    public const string FullAccess = "FullAccess";
    public const string RestrictedAccess = "RestrictedAccess";
    public const string FullRead = "FullRead";
    public const string RestrictedRead = "RestrictedRead";
}

public static class Constants
{
    public const string CreatedBy = "createdBy";
}

public enum AccessLevel
{
    Full,
    Restricted,
}