using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.MonoApp;

public class MonoRunTime
{
    public string AppRoot { get; set; } =  Path.Join(Directory.GetCurrentDirectory(), "wwwroot/apps"); 
}

public record Spa(string Path, string Dir);

public record MonoSettings(
    DatabaseProvider DatabaseProvider,
    string ConnectionString,

    Spa[]? Spas = null,
    string[]? CorsOrigins = null
);

public record SuperAdminRequest(string Email, string Password);
public record DatabaseConfigRequest(DatabaseProvider DatabaseProvider, string ConnectionString);