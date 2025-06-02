namespace FormCMS.Auth.Models;

public static class Roles
{
    /// <summary>
    /// super admin:
    /// schema: any schema,
    /// data: any entity
    /// </summary>
    public const string Sa = "sa"; 
    
    /// <summary>
    /// admin
    /// schema: only entity and view, only his own schema
    /// </summary>
    public const string Admin = "admin";
    
}