namespace Feedback360.Application.Common;

public static class DateTimeNormalization
{
    public static DateTime? ToUtc(DateTime? value)
    {
        if (value is null) return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value.ToUniversalTime()
        };
    }
}
