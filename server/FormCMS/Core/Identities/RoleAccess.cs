namespace FormCMS.Core.Identities;

public record RoleAccess(
    string Name,
    string[]? ReadWriteEntities,
    string[]? RestrictedReadWriteEntities,
    string[]? ReadonlyEntities,
    string[]? RestrictedReadonlyEntities
);
