
using System;
using System.Threading.Tasks;
using FormCMS.Subscriptions.Models;
using Xunit;

namespace FormCMS.Course.Tests;

[Collection("API")]
public class StripeSubApiTestShould(AppFactory factory)
{
    [Fact]
    public async Task CustomerShouldCreated()
    {
        //Arrange
        var expected = new Customer(
            factory.Faker.Internet.Email(),
            "pm_card_visa",
            factory.Faker.Name.FullName(),
            null
        );
        //Act
        var result = await factory.StripeSubClient.CreateCustomer(expected);
        //Assert
        var actual = await factory.StripeSubClient.GetCustomer(result.Value.Id);
        Assert.Equal(expected.Name, actual.Value.Name);
        Assert.Equal(expected.Email, actual.Value.Email);
    }

    [Fact]
    public async Task ProductShouldBeCreated()
    {
        var result = await factory.StripeSubClient.CreateProduct(
            new Product( null, "course", 1L, "USD", "week", DateTime.Now)
        );
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.ExternalId);
        Assert.Equal("USD", result.Value.Currency, true);
    }

    // [Fact]
    // public async Task SubscriptionShouldBeCreated()
    // {
    //     var result = await factory.StripeSubClient.CreateSubscription(
    //         new Subscription(
    //             null,
    //             "cus_SYSVFT9NNVnoxt",
    //             "prod_SZDL7YYCazcG8X",
    //             DateTime.Now,
    //             null,
    //             "creating",
    //             "price_1Re4uR1ULvZc5yjVkcUgbrpD"
    //         )
    //     );
    //     Assert.True(result.IsSuccess);
    // }

    // [Theory]
    // [InlineData("sub_1Re5P91ULvZc5yjVu8183hND")]
    // public async Task SubscriptionShouldBeCancelled(string subId)
    // {
    //     var sub = await factory.StripeSubClient.GetSubscription(subId);
    //     Assert.NotNull(sub);
    //
    //     await factory.StripeSubClient.CancelSubscription(sub.Value.ExternalId, default);
    //     var su = await factory.StripeSubClient.GetSubscription(subId);
    //     Assert.Equal("sub_1Re5P91ULvZc5yjVu8183hND", su.Value.ExternalId);
    //     Assert.Equal("canceled", su.Value.Status);
    // }
    //
    // [Theory]
    // [InlineData(3)]
    // public async Task ProductsShouldBeGet(int count)
    // {
    //     var prods = await factory.StripeSubClient.GetProducts(count);
    //     Assert.NotNull(prods);
    //     Assert.Equal(count, prods.Value.Count());
    // }
}
