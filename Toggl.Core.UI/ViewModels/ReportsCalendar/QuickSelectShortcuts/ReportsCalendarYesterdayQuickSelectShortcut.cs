using Toggl.Core.Analytics;
using Toggl.Core.Models;
using Toggl.Core.UI.Parameters;
using Toggl.Core.Services;
using Toggl.Shared;

namespace Toggl.Core.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts
{
    public sealed class ReportsCalendarYesterdayQuickSelectShortcut : ReportsCalendarBaseQuickSelectShortcut
    {
         public ReportsCalendarYesterdayQuickSelectShortcut(ITimeService timeService)
            : base(timeService, Resources.Yesterday, ReportPeriod.Yesterday)
        {
        }

        public override ReportsDateRange GetDateRange()
        {
            var yesterday = TimeService.CurrentDateTime.Date.AddDays(-1);
            return ReportsDateRange
                .WithDates(yesterday, yesterday)
                .WithSource(ReportsSource.ShortcutYesterday);
        }
    }
}
