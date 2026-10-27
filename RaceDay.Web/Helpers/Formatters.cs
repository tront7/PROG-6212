using System.Globalization;

namespace RaceDay.Web.Helpers;

/// <summary>Display helpers shared by the Razor views.</summary>
public static class Formatters
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>1 → "1st", 47 → "47th", 112 → "112th".</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = Math.Abs(number) % 100;
        if (lastTwo is 11 or 12 or 13) return $"{number}th";

        return (Math.Abs(number) % 10) switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }

    public static string Rand(decimal amount) => amount == 0 ? "Free" : "R" + amount.ToString("N2", Inv);

    public static string Distance(decimal km) => km.ToString("0.##", Inv) + " km";

    public static string FinishTime(TimeSpan? time)
    {
        if (time is null) return "—";
        var t = time.Value;
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    public static string ShortDate(DateTime date) => date.ToString("d MMM yyyy", Inv);

    public static string LongDate(DateTime date) => date.ToString("dddd, d MMMM yyyy", Inv);

    public static string Time(DateTime date) => date.ToString("HH:mm", Inv);

    public static string TypeIcon(string? eventType) => eventType switch
    {
        "Walk" => "🚶",
        "Cycle" => "🚴",
        _ => "🏃"
    };

    /// <summary>"Confirmed" → "status-confirmed" etc. for the colour-coded badges.</summary>
    public static string StatusClass(string? status) => "status-" + (status ?? "unknown").ToLowerInvariant();

    public static string TypeClass(string? eventType) => "type-" + (eventType ?? "run").ToLowerInvariant();
}
