using System;
using Toggl.Core.Analytics;
using Toggl.Core.Models;
using Toggl.Core.UI.Parameters;
using Toggl.Core.Services;
using Toggl.Shared;

namespace Toggl.Core.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarLastMonthQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarLastMonthQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.LastMonth, ReportPeriod.LastMonth)
        {
        }

        public override ReportsDateRange GetDateRange()
        {
            var lastMonth = TimeService.CurrentDateTime.Date.AddMonths(-1);
            var start = new DateTimeOffset(lastMonth.Year, lastMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1).AddDays(-1);
            return ReportsDateRange
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutLastMonth);
        }
    }
}
