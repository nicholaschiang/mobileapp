using System;
using Toggl.Core.Analytics;
using Toggl.Core.UI.Parameters;
using Toggl.Core.Services;
using Toggl.Shared;

namespace Toggl.Core.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarLastYearQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        public ReportsCalendarLastYearQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.LastYear, ReportPeriod.LastYear)
        {
        }

        public override ReportsDateRange GetDateRange()
        {
            var lastYear = TimeService.CurrentDateTime.Year - 1;
            var start = new DateTimeOffset(lastYear, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddYears(1).AddDays(-1);
            return ReportsDateRange
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutLastYear);
        }
    }
}
