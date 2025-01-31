using FormCMS.Core.Descriptors;

namespace FormCMS.Core.HookFactory;

public record JunctionPreAddArgs(LoadedEntity Entity, string RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs(Entity.Name) ;
public record JunctionPreDelArgs(LoadedEntity Entity, string RecordId, LoadedAttribute Attribute, Record[] RefItems):BaseArgs (Entity.Name);
