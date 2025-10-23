using FormCMS.Infrastructure.RelationDbDao;

namespace FormCMS.Activities.Models;

public record ActivityContext(ShardRouter ShardRouter, ShardGroup DefaultShardGroup);