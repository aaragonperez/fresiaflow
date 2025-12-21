namespace FresiaFlow.Domain.Shared;

/// <summary>
/// Value Object que representa un rango de fechas.
/// </summary>
public class DateRange
{
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    private DateRange() { } // EF Core

    public DateRange(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha de fin.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool Contains(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    public int Days => (EndDate - StartDate).Days;

    public static DateRange ThisMonth()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return new DateRange(start, end);
    }

    public static DateRange ThisWeek()
    {
        var now = DateTime.UtcNow;
        var start = now.AddDays(-(int)now.DayOfWeek);
        var end = start.AddDays(6);
        return new DateRange(start, end);
    }
}

