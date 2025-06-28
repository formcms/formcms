using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FormCMS.Core.Assets;
using FormCMS.Subscriptions.Models;
using FormCMS.Utils.ResultExt;
 
namespace FormCMS.Course.Tests;

[Collection("API")]
public class StripeSubApiTestShould(AppFactory factory)
{
    [Fact]
    public async Task CustomerShouldCreated()
    { 
        //Arrange 
        var expected = new StripeCustomer(factory.Faker.Internet.Email(), "pm_card_visa", factory.Faker.Name.FullName(), null);
        //Act
        var result = await factory.StripeSubClient.CreateCustomer(expected);
        //Assert
        var actual = await  factory.StripeSubClient.GetCustomer(result.Value.Id);
        Assert.Equal(expected.Name, actual.Value.Name);
        Assert.Equal(expected.Email, actual.Value.Email);
    }

    [Fact]
    public async Task ProductShouldBeCreated()
    {
        var result = await factory.StripeSubClient.CreateProduct(
            new StripeProduct("product", null, "course", 1L, "USD", "week", DateTime.Now)
        );
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Id);
        Assert.Equal("USD", result.Value.Currency,true);
    }

    [Fact]
    public async Task SubscriptionShouldBeCreated()
    {
        var result = await factory.StripeSubClient.CreateSubscription(
            new StripeSubscription(
                "Subscription",
                null,
                "cus_SYSVFT9NNVnoxt",
                "prod_SZDL7YYCazcG8X",
                DateTime.Now,
                null,
                "creating",
                "price_1Re4uR1ULvZc5yjVkcUgbrpD"
            )
        );
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("sub_1Re5P91ULvZc5yjVu8183hND")]
    public async Task SubscriptionShouldBeCancelled(string subId)
    {
        var sub = await factory.StripeSubClient.GetSubscription(subId);
        Assert.NotNull(sub);

         await factory.StripeSubClient.CancelSubscription(sub.Value.Id, default);
       var su = await factory.StripeSubClient.GetSubscription(subId);
        Assert.Equal("sub_1Re5P91ULvZc5yjVu8183hND",su.Value.Id);
        Assert.Equal("canceled", su.Value.Status);
    }
}
