using System.Collections.Immutable;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.HookFactory;
public record EntityPreGetSingleArgs(LoadedEntity Entity, string RecordId, Record? OutRecord):BaseArgs (Entity.Name);

public record EntityPreGetListArgs(LoadedEntity Entity, ImmutableArray<ValidFilter> RefFilters, ImmutableArray<ValidSort> RefSorts, ValidPagination RefPagination ):BaseArgs(Entity.Name) ;

public record EntityPreUpdateArgs(LoadedEntity Entity, Record RefRecord):BaseArgs (Entity.Name);
public record EntityPostUpdateArgs(LoadedEntity Entity, Record Record):BaseArgs (Entity.Name);

public record EntityPreAddArgs(LoadedEntity Entity, Record RefRecord):BaseArgs (Entity.Name);
public record EntityPostAddArgs(LoadedEntity Entity, Record Record):BaseArgs(Entity.Name) ;

public record EntityPreDelArgs(LoadedEntity Entity, Record RefRecord):BaseArgs (Entity.Name);
public record EntityPostDelArgs(LoadedEntity Entity, Record Record):BaseArgs (Entity.Name);
