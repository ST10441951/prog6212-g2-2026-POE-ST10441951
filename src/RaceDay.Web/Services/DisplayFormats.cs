using System.Globalization;

namespace RaceDay.Web.Services;

// Keep displayed rand amounts consistent across Windows and Linux culture data.
// Microsoft (2026): https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo
// https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
// Accessed: 19 November 2026.
public static class DisplayFormats
{
    private static readonly NumberFormatInfo RandNumbers = NumberFormatInfo.ReadOnly(new NumberFormatInfo
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = " ",
        NumberGroupSizes = [3]
    });

    public static string Rand(decimal amount) => "R " + amount.ToString("N2", RandNumbers);
}
