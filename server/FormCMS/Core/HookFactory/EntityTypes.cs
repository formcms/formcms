using System.Collections.Immutable;
using FormCMS.Core.Descriptors;
using FormCMS.Utils.DisplayModels;

namespace FormCMS.Core.HookFactory;
public record EntityPreGetSingleArgs(string Name, string RecordId, Record? OutRecord):BaseArgs (Name);
public record EntityPostGetSingleArgs(string Name, string RecordId, Record Record):BaseArgs (Name);
public record EntityPreGetListArgs(LoadedEntity Entity,
    ImmutableArray<ValidFilter> RefFilters, 
    ImmutableArray<ValidSort> RefSorts, 
    ValidPagination RefPagination, 
    ListResponseMode ListResponseMode
    ):BaseArgs(Entity.Name) ;
public record EntityPostGetListArgs(LoadedEntity Entity, ListResponse RefListResponse):BaseArgs(Entity.Name) ;
public record EntityPreUpdateArgs(string Name, string RecordId,string RecordLabel, Record RefRecord):BaseArgs (Name);
public record EntityPostUpdateArgs(string Name, string RecordId,string RecordLabel, Record Record):BaseArgs (Name);
public record EntityPreAddArgs(string Name,string RecordLabel, Record RefRecord):BaseArgs (Name);
public record EntityPostAddArgs(string Name, string RecordId, string RecordLabel, Record Record):BaseArgs(Name) ;
public record EntityPreDelArgs(string Name, string RecordId, string RecordLabel, Record RefRecord):BaseArgs (Name);
public record EntityPostDelArgs(string Name, string RecordId, string RecordLabel, Record Record):BaseArgs (Name);
