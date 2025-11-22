using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Engagements.Models;

public record EngagementContext(ShardRouter EngagementStatusShardRouter, ShardGroup EngagementCountShardGroup);