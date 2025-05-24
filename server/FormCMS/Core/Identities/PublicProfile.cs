namespace FormCMS.Core.Identities;

//can view by public, hide email
public record PublicProfile
(
        string Id,
        string Name,
        string AvatarUrl
);