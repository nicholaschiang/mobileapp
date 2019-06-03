using System;
using Toggl.Core.Analytics;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.Core.UI.Parameters
{
    public sealed class ReportsDateRange
    {
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public ReportsSource Source { get; set; }

        public static ReportsDateRange WithDates(
            DateTimeOffset start,
            DateTimeOffset end
        )
        {
            if (start > end)
                (start, end) = (end, start);

            return new ReportsDateRange { StartDate = start, EndDate = end, Source = ReportsSource.Other };
        }

        public ReportsDateRange WithSource(ReportsSource source)
        {
            return new ReportsDateRange { StartDate = this.StartDate, EndDate = this.EndDate, Source = source };
        }
    }

    public static class ReportsDateRangeExtensions
    {
        public static bool IsCurrentWeek(this ReportsDateRange range, DateTimeOffset currentTime, BeginningOfWeek beginningOfWeek)
        {
            var firstDayOfCurrentWeek = currentTime.BeginningOfWeek(beginningOfWeek);
            var lastDayOfCurrentWeek = firstDayOfCurrentWeek.AddDays(6);
            return range.StartDate == firstDayOfCurrentWeek && range.EndDate == lastDayOfCurrentWeek;
        }

        public static bool IsLastWeek(this ReportsDateRange range, DateTimeOffset currentTime, BeginningOfWeek beginningOfWeek)
        {
            var firstDayOfLastWeek = currentTime.BeginningOfWeek(beginningOfWeek).AddDays(-7);
            var lastDayOfLastWeek = firstDayOfLastWeek.AddDays(6);
            return range.StartDate == firstDayOfLastWeek && range.EndDate == lastDayOfLastWeek;
        }

        public static bool IsCurrentMonth(this ReportsDateRange range, DateTimeOffset currentTime)
        {
            var firstDayOfCurrentMonth = currentTime.RoundDownToLocalMonth();
            var lastDayOfCurrentMonth = firstDayOfCurrentMonth.AddMonths(1).AddDays(-1);
            return range.StartDate == firstDayOfCurrentMonth && range.EndDate == lastDayOfCurrentMonth;
        }

        public static bool IsLastMonth(this ReportsDateRange range, DateTimeOffset currentTime)
        {
            var firstDayOfPreviousMonth = currentTime.RoundDownToLocalMonth().AddMonths(-1);
            var lastDayOfPreviousMonth = firstDayOfPreviousMonth.AddMonths(1).AddDays(-1);
            return range.StartDate == firstDayOfPreviousMonth && range.EndDate == lastDayOfPreviousMonth;
        }

        public static bool IsCurrentYear(this ReportsDateRange range, DateTimeOffset currentTime)
        {
            var firstDayOfCurrentYear = currentTime.RoundDownToLocalYear();
            var lastDayOfCurrentYear = firstDayOfCurrentYear.AddYears(1).AddDays(-1);
            return range.StartDate == firstDayOfCurrentYear && range.EndDate == lastDayOfCurrentYear;
        }

        public static bool IsLastYear(this ReportsDateRange range, DateTimeOffset currentTime)
        {
            var firstDayOfLastYear = currentTime.RoundDownToLocalYear().AddYears(-1);
            var lastDayOfLastYear = firstDayOfLastYear.AddYears(1).AddDays(-1);
            return range.StartDate == firstDayOfLastYear && range.EndDate == lastDayOfLastYear;
        }
    }
}
