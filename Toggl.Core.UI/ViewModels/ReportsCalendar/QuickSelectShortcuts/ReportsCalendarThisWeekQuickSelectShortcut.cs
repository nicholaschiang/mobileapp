using Toggl.Core.Analytics;
using Toggl.Core.Models;
using Toggl.Core.UI.Parameters;
using Toggl.Core.Services;
using Toggl.Shared;

namespace Toggl.Core.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarThisWeekQuickSelectShortcut
        : ReportsCalendarBaseQuickSelectShortcut
    {
        private readonly BeginningOfWeek beginningOfWeek;

        public ReportsCalendarThisWeekQuickSelectShortcut
            (ITimeService timeService, BeginningOfWeek beginningOfWeek)
            : base(timeService, Resources.ThisWeek, ReportPeriod.ThisWeek)
        {
            this.beginningOfWeek = beginningOfWeek;
        }

        public override ReportsDateRange GetDateRange()
        {
            var now = TimeService.CurrentDateTime.Date;
            var difference = (now.DayOfWeek - beginningOfWeek.ToDayOfWeekEnum() + 7) % 7;
            var start = now.AddDays(-difference);
            var end = start.AddDays(6);
            return ReportsDateRange
                .WithDates(start, end)
                .WithSource(ReportsSource.ShortcutThisWeek);
        }
    }
}
