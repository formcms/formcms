using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Engagements.Models;

public record EngagementContext(ShardRouter UserActivityShardRouter, ShardGroup CountShardGroup);