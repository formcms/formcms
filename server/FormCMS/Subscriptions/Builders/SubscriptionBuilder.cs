using System.Runtime.CompilerServices;
using FormCMS.Comments.Builders;
using FormCMS.Comments.Handlers;
using FormCMS.Comments.Services;
using FormCMS.Subscriptions.Handlers;
using FormCMS.Subscriptions.Services;
using Stripe;

namespace FormCMS.Subscriptions.Builders
{
    public class SubscriptionBuilder(ILogger<SubscriptionBuilder> logger)
    {
        public static  IServiceCollection AddStripeSubscription(IServiceCollection services)
        {
            services.AddSingleton<SubscriptionBuilder>();
            services.AddSingleton<PriceService>();
            services.AddSingleton<CustomerService>();
            services.AddSingleton<SubscriptionService>();
            services.AddSingleton<ProductService>();
            services.AddSingleton<PriceService>();
            
            services.AddSingleton<IProductService,StripeProdService>();
            services.AddSingleton<PaymentMethodService>();
            services.AddSingleton<ICustomerService, StripeCustomerService>();
            services.AddScoped<ISubscriptionService, StripeSubscriptionService>();
            return services;
        }

        public WebApplication UseStripeSubscription(WebApplication app)
        {
            logger.LogInformation(
                $"""
                *********************************************************
                Using  Subscription Services
                *********************************************************
                """
            );

            var options = app.Services.GetRequiredService<SystemSettings>();
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/subscriptions").MapSubscriptionHandlers();
            return app;
        }
    }
}
