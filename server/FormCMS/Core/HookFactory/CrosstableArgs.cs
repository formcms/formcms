using FormCMS.Core.Descriptors;

namespace FormCMS.Core.HookFactory;

public record JunctionPreAddArgs(LoadedEntity Entity, ValidValue RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs(Entity.Name) ;
public record JunctionPreDelArgs(LoadedEntity Entity, ValidValue RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs (Entity.Name);
