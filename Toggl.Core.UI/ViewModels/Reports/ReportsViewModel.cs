using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.UI.Navigation;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Diagnostics;
using Toggl.Core.Interactors;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.Views;
using Toggl.Core.Reports;
using Toggl.Core.Services;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Shared.Models.Reports;
using Toggl.Networking.Exceptions;
using CommonFunctions = Toggl.Shared.Extensions.CommonFunctions;
using Colors = Toggl.Core.UI.Helper.Colors;

namespace Toggl.Core.UI.ViewModels.Reports
{
    [Preserve(AllMembers = true)]
    public sealed class ReportsViewModel : ViewModel
    {
        private const float minimumSegmentPercentageToBeOnItsOwn = 5f;
        private const float maximumSegmentPercentageToEndUpInOther = 1f;
        private const float minimumOtherSegmentDisplayPercentage = 1f;
        private const float maximumOtherProjectPercentageWithSegmentsBetweenOneAndFivePercent = 5f;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IAnalyticsService analyticsService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IIntentDonationService intentDonationService;

        private readonly BehaviorSubject<IThreadSafeWorkspace> workspaceSubject = new BehaviorSubject<IThreadSafeWorkspace>(null);
        private readonly Subject<Unit> reportSubject = new Subject<Unit>();
        private readonly BehaviorSubject<bool> isLoadingSubject = new BehaviorSubject<bool>(true);
      
        private ReportsSource source;

        [Obsolete("This should be removed, replaced by something that is actually used or turned into a constant.")]
        private int projectsNotSyncedCount = 0;

        private long userId;
        private DateTime reportSubjectStartTime;

        // Sub-ViewModels
        public ReportsBarChartViewModel BarChartViewModel { get; }
        public ReportsCalendarViewModel CalendarViewModel { get; }

        // Report observables
        public IObservable<TimeSpan> TotalTimeObservable { get; }
        public IObservable<bool> TotalTimeIsZeroObservable { get; }
        public IObservable<float?> BillablePercentageObservable { get; }
        public IObservable<DurationFormat> DurationFormatObservable { get; }
        public IObservable<IReadOnlyList<ChartSegment>> SegmentsObservable { get; private set; }
        public IObservable<IReadOnlyList<ChartSegment>> GroupedSegmentsObservable { get; private set; }
        public IObservable<bool> WorkspaceHasBillableFeatureEnabled { get; }

        // Page state
        public IObservable<bool> IsLoadingObservable { get; }
        public IObservable<bool> ShowEmptyStateObservable { get; }
        public IObservable<string> WorkspaceNameObservable { get; }
        public ICollection<SelectOption<IThreadSafeWorkspace>> Workspaces { get; private set; }
        public IObservable<ICollection<SelectOption<IThreadSafeWorkspace>>> WorkspacesObservable { get; }

        // Date Ranges
        public IObservable<DateTimeOffset> EndDate { get; }
        public IObservable<DateTimeOffset> StartDate { get; }
        public IObservable<string> CurrentDateRangeStringObservable { get; }

        public UIAction SelectWorkspace { get; }

        public ReportsViewModel(ITogglDataSource dataSource,
            ITimeService timeService,
            INavigationService navigationService,
            IInteractorFactory interactorFactory,
            IAnalyticsService analyticsService,
            IIntentDonationService intentDonationService,
            ISchedulerProvider schedulerProvider,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory rxActionFactory)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;
            this.stopwatchProvider = stopwatchProvider;
            this.intentDonationService = intentDonationService;

            CalendarViewModel = new ReportsCalendarViewModel(timeService, dataSource, intentDonationService, rxActionFactory, navigationService);

            var currentWorkspaceId = workspaceSubject
                .Select(w => w?.Id)
                .Where(id => id != null)
                .Select(id => id.Value)
                .DistinctUntilChanged();

            var totalsObservable = Observable
                .CombineLatest(
                    reportSubject,
                    CalendarViewModel.SelectedDateRangeObservable,
                    currentWorkspaceId,
                    (_, range, workspaceId) => interactorFactory
                        .GetReportsTotals(userId, workspaceId, range.StartDate, range.EndDate)
                        .Execute())
                .SelectMany(CommonFunctions.Identity)
                .Catch<ITimeEntriesTotals, OfflineException>(_ => Observable.Return<ITimeEntriesTotals>(null))
                .Where(report => report != null);

            BarChartViewModel = new ReportsBarChartViewModel(schedulerProvider, dataSource.Preferences, totalsObservable, navigationService);

            // Summary Reports
             var isLoading = isLoadingSubject
                .AsObservable()
                .StartWith(true)
                .DistinctUntilChanged();

            var summaryReportObservable = Observable
                .CombineLatest(
                    reportSubject,
                    CalendarViewModel.SelectedDateRangeObservable.Where(rangeContainsValidDates),
                    currentWorkspaceId,
                    (_, range, workspaceId) =>
                    {
                        setLoadingState();

                        return interactorFactory
                            .GetProjectSummary(workspaceId, range.StartDate, range.EndDate)
                            .Execute();
                    })
                .SelectMany(CommonFunctions.Identity)
                .Catch(Observable.Never<ProjectSummaryReport>())
                .Do(_ => isLoadingSubject.OnNext(false));

            var totalTimeObservable = summaryReportObservable
                .Select(report => TimeSpan.FromSeconds(report.TotalSeconds));

            var durationFormatObservable = dataSource.Preferences.Current
                .Select(prefs => prefs.DurationFormat);

            TotalTimeObservable = totalTimeObservable
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            TotalTimeIsZeroObservable = totalTimeObservable
                .Select(time => time.Ticks == 0)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            BillablePercentageObservable = summaryReportObservable
                .Select(report => report.TotalSeconds is 0 ? null : (float?)report.BillablePercentage)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            var segmentsObservable = isLoading
                .Where(CommonFunctions.Identity)
                .Select(_ => new ChartSegment[0])
                .Merge(summaryReportObservable.Select(report => report.Segments));

            SegmentsObservable = segmentsObservable.CombineLatest(durationFormatObservable, applyDurationFormat);
            GroupedSegmentsObservable = SegmentsObservable.CombineLatest(durationFormatObservable, groupSegments);

            // Page State
            IsLoadingObservable = isLoading.AsDriver(schedulerProvider);

            ShowEmptyStateObservable = SegmentsObservable.CombineLatest(IsLoadingObservable, shouldShowEmptyState);

            DurationFormatObservable = durationFormatObservable
                .AsDriver(schedulerProvider);

            WorkspaceNameObservable = workspaceSubject
                .Select(workspace => workspace?.Name ?? string.Empty)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            WorkspaceHasBillableFeatureEnabled = workspaceSubject
                .Where(workspace => workspace != null)
                .SelectMany(workspace => interactorFactory.GetWorkspaceFeaturesById(workspace.Id).Execute())
                .Select(workspaceFeatures => workspaceFeatures.IsEnabled(WorkspaceFeatureId.Pro))
                .StartWith(false)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);
            WorkspacesObservable = interactorFactory.ObserveAllWorkspaces().Execute()
                .Select(list => list.Where(w => !w.IsInaccessible))
                .Select(readOnlyWorkspaceSelectOptions)
                .AsDriver(schedulerProvider);

            // Date Ranges
            StartDate = CalendarViewModel
                .SelectedDateRangeObservable
                .Select(range => range.StartDate)
                .AsObservable()
                .AsDriver(schedulerProvider);

            EndDate = CalendarViewModel
                .SelectedDateRangeObservable
                .Select(range => range.EndDate)
                .AsObservable()
                .AsDriver(schedulerProvider);

            CurrentDateRangeStringObservable = 
                Observable.CombineLatest(
                    CalendarViewModel.SelectedDateRangeObservable,
                    dataSource.Preferences.Current.Select(p => p.DateFormat).DistinctUntilChanged(),
                    dataSource.User.Current.Select(currentUser => currentUser.BeginningOfWeek).DistinctUntilChanged(),
                    getCurrentDateRangeString
                ).DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            SelectWorkspace = rxActionFactory.FromAsync(selectWorkspace);

            bool rangeContainsValidDates(ReportsDateRange range)
                => range.StartDate != default(DateTimeOffset)
                && range.EndDate != default(DateTimeOffset);

            string getCurrentDateRangeString(ReportsDateRange range, DateFormat dateFormat, BeginningOfWeek beginningOfWeek)
            {
                var startDate = range.StartDate;
                var endDate = range.EndDate;

                if (startDate == default(DateTimeOffset) || endDate == default(DateTimeOffset))
                    return "";

                if (startDate == endDate)
                    return $"{startDate.ToString(dateFormat.Short, CultureInfo.InvariantCulture)} ▾";

                var firstDayOfCurrentWeek = timeService.CurrentDateTime.BeginningOfWeek(beginningOfWeek);
                var lastDayOfCurrentWeek = firstDayOfCurrentWeek.AddDays(6);

                var isCurrentWeek = startDate.Date == firstDayOfCurrentWeek && endDate.Date == lastDayOfCurrentWeek;
                if (isCurrentWeek)
                    return $"{Resources.ThisWeek} ▾";

                var formattedStartDate = startDate.ToString(dateFormat.Short, CultureInfo.InvariantCulture);
                var formattedEndDate = endDate.ToString(dateFormat.Short, CultureInfo.InvariantCulture);

                return $"{formattedStartDate} - {formattedEndDate} ▾";
            }
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            await CalendarViewModel.Initialize();

            WorkspacesObservable
                .Subscribe(data => Workspaces = data)
                .DisposedBy(disposeBag);

            userId = await dataSource.User.Get().Select(u => u.Id);

            interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("ReportsViewModel.Initialize")
                .Execute()
                .Subscribe(workspace => workspaceSubject.OnNext(workspace));
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();

            var firstTimeOpenedFromMainTabBarStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenReportsViewForTheFirstTime);
            stopwatchProvider.Remove(MeasuredOperation.OpenReportsViewForTheFirstTime);
            firstTimeOpenedFromMainTabBarStopwatch?.Stop();
            firstTimeOpenedFromMainTabBarStopwatch = null;

            intentDonationService.DonateShowReport();

            CalendarViewModel.ViewAppeared();
            reportSubject.OnNext(Unit.Default);
        }

        public void StopNavigationFromMainLogStopwatch()
        {
            var navigationStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenReportsFromGiskard);
            stopwatchProvider.Remove(MeasuredOperation.OpenReportsFromGiskard);
            navigationStopwatch?.Stop();
        }

        private static ReadOnlyCollection<SelectOption<IThreadSafeWorkspace>> readOnlyWorkspaceSelectOptions(IEnumerable<IThreadSafeWorkspace> workspaces)
            => workspaces
                .Select(ws => new SelectOption<IThreadSafeWorkspace>(ws, ws.Name))
                .ToList()
                .AsReadOnly();

        private void setLoadingState()
        {
            reportSubjectStartTime = timeService.CurrentDateTime.UtcDateTime;
            isLoadingSubject.OnNext(true);
        }

        //TODO: Reuse this
        private void trackReportsEvent(bool success)
        {
            var totalDays = 0; //TODO: Recalculate this
            var loadingTime = timeService.CurrentDateTime.UtcDateTime - reportSubjectStartTime;

            if (success)
            {
                analyticsService.ReportsSuccess.Track(source, totalDays, projectsNotSyncedCount, loadingTime.TotalMilliseconds);
            }
            else
            {
                analyticsService.ReportsFailure.Track(source, totalDays, loadingTime.TotalMilliseconds);
            }
        }

        private IReadOnlyList<ChartSegment> applyDurationFormat(IReadOnlyList<ChartSegment> chartSegments, DurationFormat durationFormat)
        {
            return chartSegments.Select(segment => segment.WithDurationFormat(durationFormat))
                .ToList()
                .AsReadOnly();
        }

        private bool shouldShowEmptyState(IReadOnlyList<ChartSegment> chartSegments, bool isLoading)
            => chartSegments.None() && !isLoading;

        private IReadOnlyList<ChartSegment> groupSegments(IReadOnlyList<ChartSegment> segments, DurationFormat durationFormat)
        {
            var groupedData = segments.GroupBy(segment => segment.Percentage >= minimumSegmentPercentageToBeOnItsOwn).ToList();

            var aboveStandAloneThresholdSegments = groupedData
                .Where(group => group.Key)
                .SelectMany(CommonFunctions.Identity)
                .ToList();

            var otherProjectsCandidates = groupedData
                .Where(group => !group.Key)
                .SelectMany(CommonFunctions.Identity)
                .ToList();

            var finalOtherProjects = otherProjectsCandidates
                .Where(segment => segment.Percentage < maximumSegmentPercentageToEndUpInOther)
                .ToList();

            var remainingOtherProjectCandidates = otherProjectsCandidates
                .Except(finalOtherProjects)
                .OrderBy(segment => segment.Percentage)
                .ToList();

            foreach (var segment in remainingOtherProjectCandidates)
            {
                finalOtherProjects.Add(segment);

                if (percentageOf(finalOtherProjects) + segment.Percentage > maximumOtherProjectPercentageWithSegmentsBetweenOneAndFivePercent)
                {
                    break;
                }
            }

            if (!finalOtherProjects.Any())
            {
                return segments;
            }

            var leftOutOfOther = remainingOtherProjectCandidates.Except(finalOtherProjects).ToList();
            aboveStandAloneThresholdSegments.AddRange(leftOutOfOther);
            var onTheirOwnSegments = aboveStandAloneThresholdSegments.OrderBy(segment => segment.Percentage).ToList();

            ChartSegment lastSegment;

            if (finalOtherProjects.Count == 1)
            {
                var singleSmallSegment = finalOtherProjects.First();
                lastSegment = new ChartSegment(
                    singleSmallSegment.ProjectName,
                    string.Empty,
                    singleSmallSegment.Percentage >= minimumOtherSegmentDisplayPercentage ? singleSmallSegment.Percentage : minimumOtherSegmentDisplayPercentage,
                    finalOtherProjects.Sum(segment => (float)segment.TrackedTime.TotalSeconds),
                    finalOtherProjects.Sum(segment => segment.BillableSeconds),
                    singleSmallSegment.Color,
                    durationFormat);
            }
            else
            {
                var otherPercentage = percentageOf(finalOtherProjects);
                lastSegment = new ChartSegment(
                    Resources.Other,
                    string.Empty,
                    otherPercentage >= minimumOtherSegmentDisplayPercentage ? otherPercentage : minimumOtherSegmentDisplayPercentage,
                    finalOtherProjects.Sum(segment => (float)segment.TrackedTime.TotalSeconds),
                    finalOtherProjects.Sum(segment => segment.BillableSeconds),
                    Colors.Reports.OtherProjectsSegmentBackground.ToHexString(),
                    durationFormat);
            }

            return onTheirOwnSegments
                .Append(lastSegment)
                .ToList()
                .AsReadOnly();
        }

        private async Task selectWorkspace()
        {
            var currentWorkspaceId = workspaceSubject.Value.Id;
            var currentWorkspaceIndex = Workspaces.IndexOf(w => w.Item.Id == currentWorkspaceId);

            var workspace = await View.Select(Resources.SelectWorkspace, Workspaces, currentWorkspaceIndex);

            if (workspace == null || workspace.Id == currentWorkspaceId) return;

            workspaceSubject.OnNext(workspace);
        }

        private float percentageOf(List<ChartSegment> list)
            => list.Sum(segment => segment.Percentage);

        private void loadReport(IThreadSafeWorkspace workspace, DateTimeOffset startDate, DateTimeOffset endDate, ReportsSource source)
        {
            this.source = source;

            workspaceSubject.OnNext(workspace);
            CalendarViewModel.ChangeRange(startDate, endDate);

            reportSubject.OnNext(Unit.Default);
        }

        public async Task LoadReport(long? workspaceId, DateTimeOffset startDate, DateTimeOffset endDate, ReportsSource source)
        {
            var getWorkspaceInteractor = workspaceId.HasValue
                ? interactorFactory.GetWorkspaceById(workspaceSubject.Value.Id)
                : interactorFactory.GetDefaultWorkspace();

            var workspace = await getWorkspaceInteractor.Execute();

            loadReport(workspace, startDate, endDate, source);
        }
    }
}
