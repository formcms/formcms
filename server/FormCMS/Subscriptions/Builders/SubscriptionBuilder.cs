using System.Text.Json;
using System.Text.Json.Serialization;
using FormCMS.Core.HookFactory;
using FormCMS.Core.Plugins;
using FormCMS.Infrastructure.RelationDbDao;
using FormCMS.Subscriptions.Handlers;
using FormCMS.Subscriptions.Models;
using FormCMS.Subscriptions.Services;
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

        public async Task UseStripeSubscriptions(WebApplication app, IServiceScope scope)
        {
            //handler
            var options = app.Services.GetRequiredService<SystemSettings>();
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/subscriptions").MapSubscriptionHandlers();
          
            app.Services.GetRequiredService<HookRegistry>().RegisterSubscriptionsHooks();
            app.Services.GetRequiredService<PluginRegistry>().RegisterSubscriptionPlugin();
            
            await scope.ServiceProvider.GetRequiredService<ShardGroup>().PrimaryDao.EnsureSubscriptionTables();

            logger.LogInformation("""
                                  *********************************************************
                                  Using Subscription  Services
                                  *********************************************************
                                  """);
        }
    }
}