using Microsoft.Extensions.Options;
using Xunit;

namespace RaceDay.Web.Tests;

// Startup option validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class ConfigurationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("relative/")]
    [InlineData("ftp://api.example.test/")]
    [InlineData("https://api.example.test")]
    [InlineData("https://user:password@api.example.test/")]
    [InlineData("https://api.example.test/?key=value")]
    [InlineData("https://api.example.test/#fragment")]
    public void InvalidApiAddresses_StopStartup(string address)
    {
        using var factory = new WebApiFactory();
        factory.Settings["Api:BaseUrl"] = address;

        Assert.Throws<OptionsValidationException>(() => factory.Client());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("121")]
    public void InvalidTimeouts_StopStartup(string timeout)
    {
        using var factory = new WebApiFactory();
        factory.Settings["Api:TimeoutSeconds"] = timeout;

        Assert.Throws<OptionsValidationException>(() => factory.Client());
    }
}
