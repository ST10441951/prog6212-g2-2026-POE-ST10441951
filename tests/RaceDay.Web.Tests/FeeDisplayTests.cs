using System.Globalization;
using System.Text.Json.Nodes;
using Xunit;

namespace RaceDay.Web.Tests;

// Check rendered fees independently of the request thread's numeric culture.
// Microsoft (2026): https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture
// Accessed: 19 November 2026.
public sealed class FeeDisplayTests
{
    [Theory]
    [InlineData("en-US", "0", "R 0,00")]
    [InlineData("fr-FR", "0", "R 0,00")]
    [InlineData("en-ZA", "0", "R 0,00")]
    [InlineData("en-US", "1234.56", "R 1 234,56")]
    [InlineData("fr-FR", "1234.56", "R 1 234,56")]
    [InlineData("en-ZA", "1234.56", "R 1 234,56")]
    public async Task EventFees_UseTheSameRandDisplayAcrossCultures(string culture, string amount, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var data = JsonNode.Parse(EventBrowsingTests.DetailsJson)!;
            data["categories"]![0]!["entryFee"] = decimal.Parse(amount, CultureInfo.InvariantCulture);
            using var factory = new WebApiFactory { Reply = _ => WebApiFactory.Json(data.ToJsonString()) };
            using var client = factory.Client();
            Assert.Contains(expected, await client.GetStringAsync("/events/2"));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
