using System.Text.Json;
using Plugin.UplayServer.Rest.Profiles;

namespace Plugin.UplayServer.Rest.Tests;

/// <summary>
/// Filling a captured response's profile id from the URL. The value is the
/// caller's and lands inside a JSON string, so a quote in it used to end the
/// string and write JSON of the caller's choosing into the response.
/// </summary>
public class CapturedRoutesTests
{
    private const string Body = """{"profileId":"{profileId}","rewards":[]}""";

    [Fact]
    public void FillsAProfileId()
    {
        string? filled = CapturedRoutes.WithProfileId(Body, "9D4B2E1A-0000-4000-8000-000000000001");

        Assert.Equal("""{"profileId":"9d4b2e1a-0000-4000-8000-000000000001","rewards":[]}""", filled);
    }

    [Theory]
    [InlineData("x\",\"admin\":true,\"y\":\"")]
    [InlineData("not-a-guid")]
    [InlineData("9d4b2e1a-0000-4000-8000-000000000001\"")]
    public void RefusesAnythingButAProfileId(string profileId)
    {
        Assert.Null(CapturedRoutes.WithProfileId(Body, profileId));
    }

    [Fact]
    public void AlwaysProducesTheCapturedShape()
    {
        using JsonDocument parsed = JsonDocument.Parse(CapturedRoutes.WithProfileId(Body, Guid.NewGuid().ToString())!);
        Assert.Equal(2, parsed.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public void LeavesABodyWithoutThePlaceholderAlone()
    {
        const string body = """{"news":[]}""";
        Assert.Equal(body, CapturedRoutes.WithProfileId(body, "anything at all"));
    }
}
