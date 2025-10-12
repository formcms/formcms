using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.AuditLogging.Services;
using FormCMS.Auth.Models;
using FormCMS.Auth.Services;
using FormCMS.Cms.Graph;
using FormCMS.Cms.Services;
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
using Microsoft.Win32;
using static System.Runtime.InteropServices.JavaScript.JSType;
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
                hookRegistry.QueryPostSingle.RegisterDynamic(
                    "*",
                    async (
                        QueryPostSingleArgs args,
                        ISubscriptionService service,
                        IProfileService profile
                    ) =>
                    {
                        if (profile.HasRole(Roles.Admin) || profile.HasRole(Roles.Sa))
                            return args;

                        foreach (var queryPluginFilter in args.Query.PluginFilters)
                        {
                            foreach (
                                var unused in from validConstraint in queryPluginFilter.Constraints
                                from validConstraintValue in validConstraint.Values
                                where validConstraintValue.S == accessLevel
                                select validConstraint
                            )
                            {
                                if (
                                    !args.RefRecord.ByJsonPath<long>(
                                        queryPluginFilter.Vector.FullPath,
                                        out var val
                                    )
                                )
                                    continue;

                                var canAccess = await service.CanAccess(
                                    "",
                                    0,
                                    val,
                                    CancellationToken.None
                                );
                                if (!canAccess)
                                {
                                    throw new ResultException(
                                        "Not have enough access level",
                                        ErrorCodes.NOT_ENOUGH_ACCESS_LEVEL
                                    );
                                }
                            }
                        }
                        return args;
                    }
                );

                hookRegistry.EntityPostAdd.RegisterDynamic(
                    "*",
                    async (
                        IProductService prodSvc,
                        KateQueryExecutor executor,
                        EntityPostAddArgs args
                    ) =>
                    {
                        var prod = await prodSvc.Add(
                            new Product(
                                args.Name,
                                (long)args.Record["Price"],
                                (string)args.Record["currency"],
                                (string)args.Record["interval"],
                                id:null
                            )
                        );
                       

                        return args;
                    }
                );

                hookRegistry.EntityPostUpdate.RegisterDynamic(
                    "*",
                    async (
                        EntityPostUpdateArgs args,
                        IProductService prodSvc,
                        IProfileService profile,
                        KateQueryExecutor executor
                    ) =>
                    {
                        if (profile.HasRole(Roles.Admin) || profile.HasRole(Roles.Sa))
                            return args;
                       
                        if (args.Record.TryGetValue("priceId", out var priceId))
                        {
                            var prodId = (string)args.Record["productId"];
                            var product = await prodSvc.ChangePrice(
                                prodId,
                                (string)priceId,
                                (long)args.Record["Price"],
                                (string)args.Record["interval"],
                                (string)args.Record["currency"],
                                (string)args.Entity.Name
                            );
                            var dic = new Dictionary<string, object>();
                            dic.Add(nameof(product.Interval).Camelize(), product.Interval);
                            dic.Add("priceId", product.DefaultPriceId);
                            dic.Add("currency", product.Currency);
                            dic.Add("productId", product.stripeId);
                            dic.Add("Price", (long)args.Record["Price"]);
                            await executor.Update((long)args.Record["id"], args.Entity.TableName, dic);
                            }
                        else
                        {
                            var prod = await prodSvc.Add(
                               new Product(
                                   args.Name,
                                   (long)args.Record["Price"],
                                   (string)args.Record["currency"],
                                   (string)args.Record["interval"]
                               )
                           );
                            var dic = new Dictionary<string, object>();
                            dic.Add(nameof(prod.Interval).Camelize(), prod.Interval);
                            dic.Add("priceId", prod.DefaultPriceId);
                            dic.Add("Price", (long)args.Record["Price"]);
                            dic.Add("currency", prod.Currency);
                            dic.Add("productId", prod.stripeId);
                            await executor.Update((long)args.Record["id"], args.Entity.TableName, dic);
                        }

                        return args;
                    }
                );

                //hookRegistry.EntityPostAdd.RegisterDynamic(
                //    "*",
                //    async (

                //        EntityPostAddArgs args,
                //        IProductService prodSvc,
                //        IPriceService priceSvc,
                //        IProfileService profile

                //    ) =>
                //    {
                //        if (profile.HasRole(Roles.Admin) || profile.HasRole(Roles.Sa))
                //            return args;
                //        var price = (long)args.Record["Price"];
                //        var priceIdAttr = args.Entity.Attributes.FirstOrDefault(x => x.ResolveVal("priceId", out var priceId));
                //        if (priceIdAttr is not null)
                //        {
                //            var prod = await prodSvc.GetByName(args.Name, CancellationToken.None);
                //            var product = await prodSvc.Add(
                //                new Product(
                //                    args.Name, // what should be extenal id
                //                    args.Entity.Name,
                //                    (long)args.Record["Price"],
                //                    (string)args.Record["Currency"],
                //                    (string)args.Record["Inteval"])

                //            , CancellationToken.None);
                //        }

                //        return args;
                //    }
                //);
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
                await dao.CreateIndex(
                    Billings.TableName,
                    [nameof(Billing.UserId).Camelize()],
                    true,
                    CancellationToken.None
                );
            }
        }
    }
}
