using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Subscriptions.Handlers;
using FormCMS.Subscriptions.Models;
using FormCMS.Subscriptions.Services;
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

            await using var scope = app.Services.CreateAsyncScope();

            var migrator = scope.ServiceProvider.GetRequiredService<DatabaseMigrator>();
            await migrator.MigrateTable(Billings.TableName, Billings.Columns);
            
            var dao = scope.ServiceProvider.GetRequiredService<IRelationDbDao>();
            await dao.CreateIndex(Billings.TableName, [nameof(Billing.UserId).Camelize()], true,CancellationToken.None);

            var options = app.Services.GetRequiredService<SystemSettings>();
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/subscriptions").MapSubscriptionHandlers();
            return app;
        }
    }
}
