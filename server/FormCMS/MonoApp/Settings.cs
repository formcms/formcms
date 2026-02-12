using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Builders;

public record Spa(string Path, string Dir);

public record Settings(
    DatabaseProvider DatabaseProvider,
    string ConnectionString,

    Spa[] Spas = null
);

public record SuperAdminRequest(string Email, string Password);
public record DatabaseConfigRequest(DatabaseProvider DatabaseProvider, string ConnectionString);