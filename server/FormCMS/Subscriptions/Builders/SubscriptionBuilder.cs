using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Core.Descriptors;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Subscriptions.Handlers;
using FormCMS.Subscriptions.Models;
using FormCMS.Subscriptions.Services;
using FormCMS.Utils.DataModels;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using BillingService = FormCMS.Subscriptions.Services.BillingService;

namespace FormCMS.Subscriptions.Builders
{
    public class SubscriptionBuilder(ILogger<SubscriptionBuilder> logger)
    {
        public static IServiceCollection AddStripeSubscription(IServiceCollection services)
        {
            services.AddSingleton<SubscriptionBuilder>();
            services.ConfigureHttpJsonOptions(AddCamelEnumConverter<SubscriptionStatus>);
            services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<IProductService, StripeProdSvcImpl>();
            services.AddScoped<ICustomerService, StripeCustomerSvcImpl>();
            services.AddScoped<ISubscriptionService, StripeSubsSvcImpl>();
            services.AddScoped<IPriceService, StripePriceSvcImpl>();
            return services;

            void AddCamelEnumConverter<T>(Microsoft.AspNetCore.Http.Json.JsonOptions options)
                where T : struct, Enum =>
                options.SerializerOptions.Converters.Add(
                    new JsonStringEnumConverter<T>(JsonNamingPolicy.CamelCase)
                );
        }

        public async Task<WebApplication> UseStripeSubscriptions(WebApplication app)
        {
            logger.LogInformation("""
                                  *********************************************************
                                  Using Subscription  Services
                                  *********************************************************
                                  """);
            RegisterHooks();
            await MigrateTables();
            MapApis();


            return app;

            void RegisterHooks()
            {
                const string accessLevel = "$access_level";
                var pluginRegistry = app.Services.GetRequiredService<PluginRegistry>();
                pluginRegistry.PluginVariables.Add(accessLevel);

                var hookRegistry = app.Services.GetRequiredService<HookRegistry>();
                hookRegistry.QueryPostSingle.RegisterDynamic("*", async (
                    QueryPostSingleArgs args,
                    ISubscriptionService service,
                    IProfileService profile
                ) =>
                {
                    if (profile.HasRole(Roles.Admin) || profile.HasRole(Roles.Sa)) return args;

                    foreach (var queryPluginFilter in args.Query.PluginFilters)
                    {
                        foreach (var validConstraint in from validConstraint in queryPluginFilter.Constraints
                                 from validConstraintValue in validConstraint.Values
                                 where validConstraintValue.S == accessLevel
                                 select validConstraint)
                        {
                            if (!args.RefRecord.ByJsonPath<long>(queryPluginFilter.Vector.FullPath, out var val))
                                continue;
                            var targetValue = validConstraint.Match == Matches.Lte ? val : val + 1;
                            var canAccess = await service.CanAccess("", 0, targetValue, CancellationToken.None);
                            if (!canAccess)
                            {
                                throw new ResultException("Not have enough access level", ErrorCodes.NOT_ENOUGH_ACCESS_LEVEL);
                            }
                        }
                    }
                    return args;
                });
            }

            void MapApis()
            {
                var options = app.Services.GetRequiredService<SystemSettings>();
                var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
                apiGroup.MapGroup("/subscriptions").MapSubscriptionHandlers();
            }

            async Task MigrateTables()
            {
                await using var scope = app.Services.CreateAsyncScope();
                var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
                await migrator.MigrateTable(Billings.TableName, Billings.Columns);

                var dao = scope.ServiceProvider.GetRequiredService<IRelationDbDao>();
                await dao.CreateIndex(Billings.TableName, [nameof(Billing.UserId).Camelize()], true,
                    CancellationToken.None);
            }
        }
    }
}