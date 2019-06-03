using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Models;
using Toggl.Core.Services;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels.ReportsCalendar;
using Toggl.Core.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.Core.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsCalendarViewModel : ViewModel
    {
        public const int MonthsToShow = 25;
        private static readonly string[] dayHeaders =
        {
            Resources.SundayInitial,
            Resources.MondayInitial,
            Resources.TuesdayInitial,
            Resources.WednesdayInitial,
            Resources.ThursdayInitial,
            Resources.FridayInitial,
            Resources.SaturdayInitial
        };

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly ISubject<Unit> reloadSubject = new Subject<Unit>();
        private readonly BehaviorSubject<int> currentPageSubject = new BehaviorSubject<int>(MonthsToShow - 1);
        private readonly ISubject<ReportsDateRange> selectedDateRangeSubject = new Subject<ReportsDateRange>();
        private readonly ISubject<ReportsDateRange> highlightedDateRangeSubject =
            new BehaviorSubject<ReportsDateRange>(default(ReportsDateRange));
 
        private ReportsCalendarDayViewModel startOfSelection;

        public IObservable<int> RowsInCurrentMonthObservable { get; private set; }
        public IObservable<CalendarMonth> CurrentMonthObservable { get; private set; }



        public int CurrentPage => currentPageSubject.Value;

        private readonly ISubject<int> monthSubject = new Subject<int>();

        public IObservable<Unit> ReloadObservable { get; }
        public IObservable<int> CurrentPageObservable { get; }
        public IObservable<ReportsDateRange> SelectedDateRangeObservable { get; }
        public IObservable<ReportsDateRange> HighlightedDateRangeObservable { get; }

        public List<ReportsCalendarBaseQuickSelectShortcut> QuickSelectShortcuts { get; private set; }

        public IObservable<List<ReportsCalendarBaseQuickSelectShortcut>> QuickSelectShortcutsObservable { get; private set; }

        public IObservable<List<ReportsCalendarPageViewModel>> MonthsObservable { get; private set; }

        public IObservable<IReadOnlyList<string>> DayHeadersObservable { get; private set; }

        public readonly InputAction<ReportsCalendarDayViewModel> SelectDay;

        public readonly InputAction<ReportsCalendarBaseQuickSelectShortcut> SelectShortcut;

        public ReportsCalendarViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IRxActionFactory rxActionFactory,
            INavigationService navigationService)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;

            SelectDay = rxActionFactory.FromAsync<ReportsCalendarDayViewModel>(calendarDayTapped);
            SelectShortcut = rxActionFactory.FromAction<ReportsCalendarBaseQuickSelectShortcut>(quickSelect);

            ReloadObservable = reloadSubject.AsObservable();
            CurrentPageObservable = currentPageSubject.AsObservable();
            SelectedDateRangeObservable = selectedDateRangeSubject.ShareReplay(1);
            HighlightedDateRangeObservable = highlightedDateRangeSubject.ShareReplay(1);

            var now = timeService.CurrentDateTime;
            var initialMonth = new CalendarMonth(now.Year, now.Month).AddMonths(-(MonthsToShow - 1));

            var beginningOfWeekObservable = dataSource.User.Current
                .Select(user => user.BeginningOfWeek)
                .DistinctUntilChanged();

            DayHeadersObservable = beginningOfWeekObservable.Select(mapDayHeaders);

            MonthsObservable = beginningOfWeekObservable.CombineLatest(
                timeService.MidnightObservable.StartWith(timeService.CurrentDateTime),
                createCalendarPages)
                .Select(pages => pages.ToList());

            RowsInCurrentMonthObservable = MonthsObservable.CombineLatest(
                CurrentPageObservable.DistinctUntilChanged(),
                (months, page) => months[page].RowCount)
                .Select(CommonFunctions.Identity);

            CurrentMonthObservable = monthSubject
                .AsObservable()
                .StartWith(MonthsToShow - 1)
                .Select(convertPageIndexToCalendarMonth)
                .Share();

            QuickSelectShortcutsObservable = beginningOfWeekObservable
                .Select(createQuickSelectShortcuts);

            CalendarMonth convertPageIndexToCalendarMonth(int pageIndex)
                => initialMonth.AddMonths(pageIndex);

            IEnumerable<ReportsCalendarPageViewModel> createCalendarPages(BeginningOfWeek beginningOfWeek, DateTimeOffset today)
            {
                var monthIterator = new CalendarMonth(today.Year, today.Month).AddMonths(-(MonthsToShow - 1));

                for (int i = 0; i < MonthsToShow; i++, monthIterator = monthIterator.Next())
                    yield return new ReportsCalendarPageViewModel(
                        monthIterator,
                        beginningOfWeek,
                        today
                    );
            }
        }

        public override async Task Initialize()
        {
            QuickSelectShortcuts = await dataSource
                .User.Current.FirstAsync()
                .Select(user => createQuickSelectShortcuts(user.BeginningOfWeek))
                .ToTask()
                .ConfigureAwait(false);

            var initialShortcut = QuickSelectShortcuts.Single(shortcut => shortcut.Period == ReportPeriod.ThisWeek);
            var initialRange = initialShortcut.GetDateRange().WithSource(ReportsSource.Initial);
            selectedDateRangeSubject.OnNext(initialRange);
            highlightedDateRangeSubject.OnNext(initialRange);

            await base.Initialize();
        }

        public void SetCurrentPage(int newPage)
        {
            currentPageSubject.OnNext(newPage);
        }

        public void UpdateMonth(int newPage)
        {
            monthSubject.OnNext(newPage);
        }

        public void Reload()
        { 
            reloadSubject.OnNext(Unit.Default);
        }

        private async Task calendarDayTapped(ReportsCalendarDayViewModel tappedDay)
        {
            if (startOfSelection == null)
            {
                var date = tappedDay.DateTimeOffset;

                var dateRange = ReportsDateRange
                    .WithDates(date, date)
                    .WithSource(ReportsSource.Calendar);
                startOfSelection = tappedDay;
                highlightDateRange(dateRange);
            }
            else
            {
                var startDate = startOfSelection.DateTimeOffset;
                var endDate = tappedDay.DateTimeOffset;

                if (System.Math.Abs((endDate - startDate).Days) > 365)
                {
                    await View.Alert(
                        Resources.ReportTooLongTitle,
                        Resources.ReportTooLongDescription,
                        Resources.Ok
                    );
                }
                else
                {
                    var dateRange = ReportsDateRange
                        .WithDates(startDate, endDate)
                        .WithSource(ReportsSource.Calendar);
                    startOfSelection = null;
                    changeDateRange(dateRange);
                }
            }
        }

        private IReadOnlyList<string> mapDayHeaders(BeginningOfWeek newBeginningOfWeek)
        {
            var updatedDayHeaders = new List<string>();
            for (var i = 0; i < dayHeaders.Length; i++)
            {
                updatedDayHeaders.Add(dayHeaderFor(i, newBeginningOfWeek));
            }

            return updatedDayHeaders.AsReadOnly();
        }

        private string dayHeaderFor(int index, BeginningOfWeek newBeginningOfWeek)
            => dayHeaders[(index + (int)newBeginningOfWeek + 7) % 7];

        public void SelectStartOfSelectionIfNeeded()
        {
            if (startOfSelection == null) return;

            var date = startOfSelection.DateTimeOffset;
            var dateRange = ReportsDateRange
                .WithDates(date, date)
                .WithSource(ReportsSource.Calendar);
            changeDateRange(dateRange);
        }

        private List<ReportsCalendarBaseQuickSelectShortcut> createQuickSelectShortcuts(BeginningOfWeek beginningOfWeek)
            => new List<ReportsCalendarBaseQuickSelectShortcut>
            {
                new ReportsCalendarTodayQuickSelectShortcut(timeService),
                new ReportsCalendarYesterdayQuickSelectShortcut(timeService),
                new ReportsCalendarThisWeekQuickSelectShortcut(timeService, beginningOfWeek),
                new ReportsCalendarLastWeekQuickSelectShortcut(timeService, beginningOfWeek),
                new ReportsCalendarThisMonthQuickSelectShortcut(timeService),
                new ReportsCalendarLastMonthQuickSelectShortcut(timeService),
                new ReportsCalendarThisYearQuickSelectShortcut(timeService),
                new ReportsCalendarLastYearQuickSelectShortcut(timeService)
            };

        private void changeDateRange(ReportsDateRange newDateRange)
        {
            startOfSelection = null;

            highlightDateRange(newDateRange);

            selectedDateRangeSubject.OnNext(newDateRange);
        }

        private void quickSelect(ReportsCalendarBaseQuickSelectShortcut quickSelectShortCut)
        {
            changeDateRange(quickSelectShortCut.GetDateRange());
        }

        private void highlightDateRange(ReportsDateRange dateRange)
        {
            highlightedDateRangeSubject.OnNext(dateRange);
        }

        internal void ChangeRange(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            var dateRange = ReportsDateRange
                .WithDates(startDate, endDate)
                .WithSource(ReportsSource.Other);

            changeDateRange(dateRange);
        }
    }
}
