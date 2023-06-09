using System;
using System.Text;

namespace PvPStats.Extensions;

internal static class TimeSpanExtensions
{
    public static string MinutesAndSeconds(this TimeSpan timeSpan)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(timeSpan.Minutes);
        stringBuilder.Append(':');
        if (timeSpan.Seconds < 10)
            stringBuilder.Append('0');
        stringBuilder.Append(timeSpan.Seconds);
        
        return stringBuilder.ToString();
    }

    public static string HoursMinutesAndSeconds(this TimeSpan timeSpan)
    {
        var stringBuilder = new StringBuilder();
        if (timeSpan.TotalHours > 1)
        {
            stringBuilder.Append(timeSpan.Hours);
            stringBuilder.Append(':');
        }

        stringBuilder.Append(timeSpan.Minutes);
        stringBuilder.Append(':');
        if (timeSpan.Seconds < 10)
            stringBuilder.Append('0');
        stringBuilder.Append(timeSpan.Seconds);
        
        return stringBuilder.ToString();
    }
}
