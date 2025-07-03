using System.Runtime.CompilerServices;
using FormCMS.Comments.Builders;
using FormCMS.Comments.Handlers;
using FormCMS.Comments.Services;
using FormCMS.Subscriptions.Handlers;

using FormCMS.Subscriptions.Services;
using Stripe;

namespace FormCMS.Subscriptions.Builders
{
    public   class SubscriptionBuilder(ILogger<CommentBuilder> logger)
    {
    
        public static  IServiceCollection AddStripeSubscription( IServiceCollection services)
        {
            services.AddSingleton<SubscriptionBuilder>();
            services.AddScoped<PriceService>();
            services.AddScoped<CustomerService>();
            services.AddScoped<SubscriptionService>();
            services.AddScoped<ProductService>();
            services.AddScoped<PriceService>();
            services.AddScoped<IProductService,StripeProdService>();
            services.AddScoped<PaymentMethodService>();
            services.AddScoped<ICustomerService, StripeCustomerService>();
            services.AddScoped<ISubscriptionService, StripeSubsService>();
            return services;
        }

        public   async  Task<WebApplication> UseStripeSubscriptions(WebApplication app)
        {
            logger.LogInformation("""
             *********************************************************
             Using Subscription  Services
             *********************************************************
             """);

            var options = app.Services.GetRequiredService<SystemSettings>();
            var apiGroup = app.MapGroup(options.RouteOptions.ApiBaseUrl);
            apiGroup.MapGroup("/subscriptions").MapSubscriptionHandlers();
          return   await Task.FromResult(app);
            
        }
    }
}
