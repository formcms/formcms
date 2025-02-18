using FormCMS.Auth.Services;
using FormCMS.Core.Identities;

namespace FormCMS.Auth.Handlers;

public static class AccountHandlers
{
    public static void MapAccountHandlers(this RouteGroupBuilder app)
    {
        app.MapGet("/users", (IAccountService svc, CancellationToken ct) => svc.GetUsers(ct));

        app.MapGet("/users/{id}", (IAccountService svc, string id, CancellationToken ct) => svc.GetSingleUser(id, ct));

        app.MapDelete("/users/{id}", (IAccountService svc, string id) => svc.DeleteUser(id));

        app.MapPost("/users", (IAccountService svc, UserAccess dto) => svc.SaveUser(dto));

        app.MapGet("/roles", (IAccountService svc, CancellationToken ct) => svc.GetRoles(ct));

        app.MapGet("/roles/{name}", (IAccountService svc, string name) => svc.GetSingleRole(name));

        app.MapPost("/roles", (IAccountService svc, RoleAccess dto) => svc.SaveRole(dto));

        app.MapDelete("/roles/{name}", (IAccountService svc, string name) => svc.DeleteRole(name));
        
        app.MapGet("/entities",(IAccountService svc, CancellationToken ct) => svc.GetEntities(ct));
    }
}
