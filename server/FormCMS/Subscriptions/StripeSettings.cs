namespace FormCMS.Subscriptions;

public sealed class StripeSettings
{
    public string SecretKey { get; set; } = "";
    public string PublishableKey { get; set; } = "";
    public string Domain { get; set; } = "";
}