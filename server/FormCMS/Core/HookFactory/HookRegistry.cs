namespace FormCMS.Core.HookFactory;
public class HookRegistry
{
        public HookList<SchemaPreGetAllArgs> SchemaPreGetAll { get; } = new();
        public HookList<SchemaPostGetSingleArgs> SchemaPostGetSingle { get; } = new();
        public HookList<SchemaPreSaveArgs> SchemaPreSave { get; } = new();
        public HookList<SchemaPostSaveArgs> SchemaPostSave { get; } = new();
        public HookList<SchemaPreDelArgs> SchemaPreDel { get; } = new();
        public HookList<QueryPreListArgs> QueryPreList { get; } = new();
        public HookList<QueryPostListArgs> QueryPostList { get; } = new();
        public HookList<QueryPreSingleArgs> QueryPreSingle { get; } = new();
        public HookList<QueryPostSingleArgs> QueryPostSingle { get; } = new();
        public HookList<QueryPartialArgs>  QueryPartial{ get; } = new();
        public HookList<ExtendingGraphQlFieldArgs> ExtendEntity { get; } = new();
        public HookList<BuildInQueryArgs> BuildInQueryArgs { get; } = new();
        public HookList<EntityPreGetSingleArgs> EntityPreGetSingle { get; } = new();
        public HookList<EntityPreGetListArgs> EntityPreGetList { get; } = new();
        public HookList<EntityPreUpdateArgs> EntityPreUpdate { get; } = new();
        public HookList<EntityPostUpdateArgs> EntityPostUpdate { get; } = new();
        public HookList<EntityPreDelArgs> EntityPreDel { get; } = new();
        public HookList<EntityPostDelArgs> EntityPostDel { get; } = new();
        public HookList<EntityPreAddArgs> EntityPreAdd { get; } = new();
        public HookList<EntityPostAddArgs> EntityPostAdd { get; } = new();
        public HookList<JunctionPreAddArgs> JunctionPreAdd { get; } = new();
        public HookList<JunctionPreDelArgs> JunctionPreDel { get; } = new();
        public HookList<AssetPreListArgs> AssetPreList { get; } = new();
        public HookList<AssetPreSingleArgs> AssetPreSingle { get; } = new();
        public HookList<AssetPreUpdateArgs> AssetPreUpdate { get; } = new();
        
        public HookList<AssetPreAddArgs> AssetPreAdd { get; } = new();
}