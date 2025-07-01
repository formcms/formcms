using System.Collections.Immutable;
using FormCMS.Core.Descriptors;

namespace FormCMS.Core.HookFactory;
public record SchemaPreGetAllArgs() : BaseArgs("");
public record SchemaPostGetHistoryArgs(ImmutableArray<Schema> Schemas) : BaseArgs("");
public record SchemaPostGetSingleArgs(Schema Schema) : BaseArgs(Schema.Name);
public record SchemaPreSaveArgs(Schema RefSchema ) : BaseArgs(RefSchema.Name);
public record SchemaPostSaveArgs(Schema Schema ) : BaseArgs(Schema.Name);
public record SchemaPreDelArgs(Schema Schema) : BaseArgs(Schema.Name);
