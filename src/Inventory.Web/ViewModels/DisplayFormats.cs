using System.Globalization;

namespace Inventory.Web.ViewModels;

internal static class DisplayFormats
{
    // Times are stored and shown in UTC, labelled as such, so they never depend on the server's time zone.
    public static string Utc(DateTime utc) =>
        utc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";
}
