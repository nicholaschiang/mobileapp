using System;
using Toggl.Core.Analytics;

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
}
