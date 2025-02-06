using FormCMS.Utils.StrArgsExt;

namespace FormCMS.Core.Descriptors;

public enum PublicationStatus
{
    Draft,
    Published,
    Unpublished,
    Scheduled
}


public enum SpecialQueryKeys
{
    Preview,
    Sandbox
}

public static class PublicationStatusHelper
{
    public static PublicationStatus? GetDataStatus(StrArgs args) =>
        args.ContainsEnumKey(SpecialQueryKeys.Preview) ? null : PublicationStatus.Published;
    public static PublicationStatus? GetSchemaStatus(StrArgs args) =>
        args.ContainsEnumKey(SpecialQueryKeys.Sandbox) ? null : PublicationStatus.Published;

}