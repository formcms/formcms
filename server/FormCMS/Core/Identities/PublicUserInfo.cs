using FormCMS.Core.Descriptors;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Utils.EnumExt;
using Humanizer;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Core.Identities;

//can view by public, hide sensitive info like email etc. 
public record PublicUserInfo(
    string Id,
    string Name,
    string AvatarUrl
);

public static class PublicUserInfos
{
    public static Entity Entity = new (
        Attributes: [
            new Attribute(Field:nameof(PublicUserInfo.Id).Camelize() ),
            new Attribute(Field:nameof(PublicUserInfo.Name).Camelize() ),
            new Attribute(Field:nameof(PublicUserInfo.AvatarUrl).Camelize() ),
            
            //to allow toLoadedAttribute() pass
            new Attribute(DefaultAttributeNames.PublicationStatus.Camelize()),
            new Attribute(DefaultColumnNames.UpdatedAt.Camelize()),
        ],
        Name: "publicUserProfile",
        DisplayName: "Public User Profile",
        TableName: "",
        LabelAttributeName: nameof(PublicUserInfo.Name).Camelize(),
        PrimaryKey: nameof(PublicUserInfo.Id).Camelize()
    );
}