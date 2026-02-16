using NUlid;

namespace FormCMS.Utils.FancyId;
public static class GuestIdentityFactory
{
    private static readonly string[] Adjectives =
    {
        "swift", "silent", "golden", "bright", "gentle",
        "calm", "frozen", "wild", "lucky", "cosmic"
    };

    private static readonly string[] Nouns =
    {
        "lotus", "panda", "harbor", "falcon", "maple",
        "ocean", "bamboo", "comet", "forest", "zen"
    };

    private static readonly Random _random = new();

    public static (string NameIdentifier, string Email) Create()
    {
        var id = Ulid.NewUlid();
        var shortId = id.ToString()[^4..]; // last 4 chars

        var adjective = Adjectives[_random.Next(Adjectives.Length)];
        var noun = Nouns[_random.Next(Nouns.Length)];

        var email = $"{adjective}-{noun}-{shortId}@cms.com";

        return ($"guest:{id}", email);
    }
}
