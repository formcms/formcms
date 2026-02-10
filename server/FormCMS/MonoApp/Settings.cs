using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Builders;

public record Spa(string Path, string Dir);

public record Settings(
    DatabaseProvider DatabaseProvider,
    string ConnectionString,
    string MasterPassword = null,
    Spa[] Spas = null
);

public record SuperAdminRequest(string Email, string Password);