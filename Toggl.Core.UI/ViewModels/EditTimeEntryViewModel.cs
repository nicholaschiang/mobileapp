using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Toggl.Core.DTOs;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.Views;
using Toggl.Core.Sync;
using Toggl.Core.Services;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Shared.Extensions.Reactive;
using Toggl.Storage.Settings;

namespace Toggl.Core.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditTimeEntryViewModel : ViewModelWithInput<long[]>
    {
        internal static readonly int MaxTagLength = 30;

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ISyncManager syncManager;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory actionFactory;
        public IOnboardingStorage OnboardingStorage { get; private set; }

        private IStopwatch stopwatchFromCalendar;
        private IStopwatch stopwatchFromMainLog;

        private long workspaceId;
        private long? projectId;
        private long? taskId;
        private IThreadSafeTimeEntry originalTimeEntry;
        private BehaviorSubject<long?> workspaceIdSubject = new BehaviorSubject<long?>(null);

        public long[] TimeEntryIds { get; set; }
        public long TimeEntryId => TimeEntryIds.First();

        public bool IsEditingGroup => TimeEntryIds.Length > 1;
        public int GroupCount => TimeEntryIds.Length;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        private BehaviorSubject<bool> isEditingDescriptionSubject;
        public BehaviorRelay<string> Description { get; private set; }

        private BehaviorSubject<ProjectClientTaskInfo> projectClientTaskSubject;
        public IObservable<ProjectClientTaskInfo> ProjectClientTask { get; private set; }

        public IObservable<bool> IsBillableAvailable { get; private set; }

        private BehaviorSubject<bool> isBillableSubject;
        public IObservable<bool> IsBillable { get; private set; }

        private BehaviorSubject<DateTimeOffset> startTimeSubject;
        public IObservable<DateTimeOffset> StartTime { get; private set; }

        private ISubject<TimeSpan?> durationSubject;
        public IObservable<TimeSpan> Duration { get; private set; }

        public IObservable<DateTimeOffset?> StopTime { get; private set; }

        private bool isRunning;
        public IObservable<bool> IsTimeEntryRunning { get; private set; }

        public TimeSpan GroupDuration { get; private set; }

        private BehaviorSubject<IEnumerable<IThreadSafeTag>> tagsSubject;
        public IObservable<IEnumerable<string>> Tags { get; set; }
        private IEnumerable<long> tagIds
            => tagsSubject.Value.Select(tag => tag.Id);

        private BehaviorSubject<bool> isInaccessibleSubject;
        public IObservable<bool> IsInaccessible { get; private set; }

        private BehaviorSubject<string> syncErrorMessageSubject;
        public IObservable<string> SyncErrorMessage { get; private set; }
        public IObservable<bool> IsSyncErrorMessageVisible { get; private set; }

        public IObservable<IThreadSafePreferences> Preferences { get; private set; }

        public OutputAction<bool> Close { get; private set; }
        public UIAction SelectProject { get; private set; }
        public UIAction SelectTags { get; private set; }
        public UIAction ToggleBillable { get; private set; }
        public InputAction<EditViewTapSource> EditTimes { get; private set; }
        public UIAction SelectStartDate { get; }
        public UIAction StopTimeEntry { get; private set; }
        public UIAction DismissSyncErrorMessage { get; private set; }
        public UIAction Save { get; private set; }
        public UIAction Delete { get; private set; }

        public EditTimeEntryViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            ISyncManager syncManager,
            IInteractorFactory interactorFactory,
            INavigationService navigationService,
            IOnboardingStorage onboardingStorage,
            IAnalyticsService analyticsService,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory actionFactory,
            ISchedulerProvider schedulerProvider)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(actionFactory, nameof(actionFactory));

            this.dataSource = dataSource;
            this.syncManager = syncManager;
            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.analyticsService = analyticsService;
            this.stopwatchProvider = stopwatchProvider;
            this.schedulerProvider = schedulerProvider;
            this.actionFactory = actionFactory;
            OnboardingStorage = onboardingStorage;

            workspaceIdSubject
                .Where(id => id.HasValue)
                .Subscribe(id => workspaceId = id.Value)
                .DisposedBy(disposeBag);

            isEditingDescriptionSubject = new BehaviorSubject<bool>(false);
            Description = new BehaviorRelay<string>(string.Empty, CommonFunctions.Trim);

            projectClientTaskSubject = new BehaviorSubject<ProjectClientTaskInfo>(ProjectClientTaskInfo.Empty);
            ProjectClientTask = projectClientTaskSubject
                .AsDriver(ProjectClientTaskInfo.Empty, schedulerProvider);

            IsBillableAvailable = workspaceIdSubject
                .Where(id => id.HasValue)
                .SelectMany(workspaceId => interactorFactory.IsBillableAvailableForWorkspace(workspaceId.Value).Execute())
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            isBillableSubject = new BehaviorSubject<bool>(false);
            IsBillable = isBillableSubject
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            startTimeSubject = new BehaviorSubject<DateTimeOffset>(DateTimeOffset.UtcNow);
            var startTimeObservable = startTimeSubject.DistinctUntilChanged();
            StartTime = startTimeObservable
                .AsDriver(default(DateTimeOffset), schedulerProvider);

            var now = timeService.CurrentDateTimeObservable.StartWith(timeService.CurrentDateTime);
            durationSubject = new ReplaySubject<TimeSpan?>(bufferSize: 1);
            Duration =
                durationSubject
                    .Select(duration
                        => duration.HasValue
                            ? Observable.Return(duration.Value)
                            : now.CombineLatest(
                                startTimeObservable,
                                (currentTime, startTime) => currentTime - startTime))
                    .Switch()
                    .DistinctUntilChanged()
                    .AsDriver(TimeSpan.Zero, schedulerProvider);

            var stopTimeObservable = Observable.CombineLatest(startTimeObservable, durationSubject, calculateStopTime)
                .DistinctUntilChanged();
            StopTime = stopTimeObservable
                .AsDriver(null, schedulerProvider);

            var isTimeEntryRunningObservable = stopTimeObservable
                .Select(stopTime => !stopTime.HasValue)
                .Do(value => isRunning = value)
                .DistinctUntilChanged();

            IsTimeEntryRunning = isTimeEntryRunningObservable
                .AsDriver(false, schedulerProvider);

            tagsSubject = new BehaviorSubject<IEnumerable<IThreadSafeTag>>(Enumerable.Empty<IThreadSafeTag>());
            Tags = tagsSubject
                .Select(tags => tags.Select(ellipsize).ToImmutableList())
                .AsDriver(ImmutableList<string>.Empty, schedulerProvider);

            isInaccessibleSubject = new BehaviorSubject<bool>(false);
            IsInaccessible = isInaccessibleSubject
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            syncErrorMessageSubject = new BehaviorSubject<string>(string.Empty);
            SyncErrorMessage = syncErrorMessageSubject
                .Select(error => error ?? string.Empty)
                .DistinctUntilChanged()
                .AsDriver(string.Empty, schedulerProvider);

            IsSyncErrorMessageVisible = syncErrorMessageSubject
                .Select(error => !string.IsNullOrEmpty(error))
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            Preferences = interactorFactory.GetPreferences().Execute()
                .AsDriver(null, schedulerProvider);

            // Actions
            Close = actionFactory.FromAsync(closeWithConfirmation);
            SelectProject = actionFactory.FromAsync(selectProject);
            SelectTags = actionFactory.FromAsync(selectTags);
            ToggleBillable = actionFactory.FromAction(toggleBillable);
            EditTimes = actionFactory.FromAsync<EditViewTapSource>(editTimes);
            SelectStartDate = actionFactory.FromAsync(selectStartDate);
            StopTimeEntry = actionFactory.FromAction(stopTimeEntry, isTimeEntryRunningObservable);
            DismissSyncErrorMessage = actionFactory.FromAction(dismissSyncErrorMessage);
            Save = actionFactory.FromAsync(save);
            Delete = actionFactory.FromAsync(delete);
        }

        public override async Task Initialize(long[] timeEntryIds)
        {
            await base.Initialize(timeEntryIds);

            if (timeEntryIds == null || timeEntryIds.Length == 0)
                throw new ArgumentException("Edit view has no Time Entries to edit.");

            TimeEntryIds = timeEntryIds;

            stopwatchFromCalendar = stopwatchProvider.Get(MeasuredOperation.EditTimeEntryFromCalendar);
            stopwatchProvider.Remove(MeasuredOperation.EditTimeEntryFromCalendar);
            stopwatchFromMainLog = stopwatchProvider.Get(MeasuredOperation.EditTimeEntryFromMainLog);
            stopwatchProvider.Remove(MeasuredOperation.EditTimeEntryFromMainLog);

            var timeEntries = await interactorFactory.GetMultipleTimeEntriesById(TimeEntryIds).Execute();
            var timeEntry = timeEntries.First();
            originalTimeEntry = timeEntry;

            projectId = timeEntry.Project?.Id;
            taskId = timeEntry.Task?.Id;
            workspaceIdSubject.OnNext(timeEntry.WorkspaceId);

            Description.Accept(timeEntry.Description);

            projectClientTaskSubject.OnNext(new ProjectClientTaskInfo(
                timeEntry.Project?.DisplayName(),
                timeEntry.Project?.DisplayColor(),
                timeEntry.Project?.Client?.Name,
                timeEntry.Task?.Name));

            isBillableSubject.OnNext(timeEntry.Billable);

            startTimeSubject.OnNext(timeEntry.Start);

            durationSubject.OnNext(timeEntry.TimeSpanDuration());

            GroupDuration = timeEntries.Sum(entry => entry.TimeSpanDuration());

            tagsSubject.OnNext(timeEntry.Tags?.ToImmutableList() ?? ImmutableList<IThreadSafeTag>.Empty);

            isInaccessibleSubject.OnNext(timeEntry.IsInaccessible);

            setupSyncError(timeEntries);
        }

        private void setupSyncError(IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            var errorCount = timeEntries.Count(te => te.IsInaccessible || !string.IsNullOrEmpty(te.LastSyncErrorMessage));

            if (errorCount == 0)
                return;

            if (IsEditingGroup)
            {
                var message = string.Format(Resources.TimeEntriesGroupSyncErrorMessage, errorCount, TimeEntryIds.Length);
                syncErrorMessageSubject.OnNext(message);

                return;
            }

            var timeEntry = timeEntries.First();

            syncErrorMessageSubject.OnNext(
                timeEntry.IsInaccessible
                ? Resources.InaccessibleTimeEntryErrorMessage
                : timeEntry.LastSyncErrorMessage);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();

            stopwatchFromCalendar?.Stop();
            stopwatchFromCalendar = null;

            stopwatchFromMainLog?.Stop();
            stopwatchFromMainLog = null;
        }

        public override void ViewDestroyed()
        {
            base.ViewDestroyed();

            disposeBag?.Dispose();
        }

        private DateTimeOffset? calculateStopTime(DateTimeOffset start, TimeSpan? duration)
            => duration.HasValue ? start + duration : null;

        private static string ellipsize(IThreadSafeTag tag)
        {
            var tagLength = tag.Name.LengthInGraphemes();
            if (tagLength <= MaxTagLength)
                return tag.Name;

            return $"{tag.Name.UnicodeSafeSubstring(0, MaxTagLength)}...";
        }

        private async Task selectProject()
        {
            analyticsService.EditEntrySelectProject.Track();
            analyticsService.EditViewTapped.Track(EditViewTapSource.Project);

            OnboardingStorage.SelectsProject();

            var selectProjectStopwatch = stopwatchProvider.CreateAndStore(
                MeasuredOperation.OpenSelectProjectFromEditView, true);

            selectProjectStopwatch.Start();

            var chosenProject = await Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                                    new SelectProjectParameter(projectId, taskId, workspaceId));

            if (chosenProject.WorkspaceId == workspaceId
                && chosenProject.ProjectId == projectId
                && chosenProject.TaskId == taskId)
                return;

            projectId = chosenProject.ProjectId;
            taskId = chosenProject.TaskId;

            if (projectId == null)
            {
                projectClientTaskSubject.OnNext(ProjectClientTaskInfo.Empty);

                clearTagsIfNeeded(workspaceId, chosenProject.WorkspaceId);
                workspaceIdSubject.OnNext(chosenProject.WorkspaceId);

                var workspace = await interactorFactory.GetWorkspaceById(chosenProject.WorkspaceId).Execute();
                isInaccessibleSubject.OnNext(workspace.IsInaccessible);

                return;
            }

            var project = await interactorFactory.GetProjectById(projectId.Value).Execute();
            clearTagsIfNeeded(workspaceId, project.WorkspaceId);

            var taskName = chosenProject.TaskId.HasValue
                ? (await interactorFactory.GetTaskById(taskId.Value).Execute())?.Name
                : string.Empty;

            projectClientTaskSubject.OnNext(new ProjectClientTaskInfo(
                project.DisplayName(),
                project.DisplayColor(),
                project.Client?.Name,
                taskName));

            workspaceIdSubject.OnNext(chosenProject.WorkspaceId);

            isInaccessibleSubject.OnNext(project.IsInaccessible);
        }

        private void clearTagsIfNeeded(long currentWorkspaceId, long newWorkspaceId)
        {
            if (currentWorkspaceId == newWorkspaceId)
                return;

            tagsSubject.OnNext(ImmutableList<IThreadSafeTag>.Empty);
        }

        private async Task selectTags()
        {
            analyticsService.EditEntrySelectTag.Track();
            analyticsService.EditViewTapped.Track(EditViewTapSource.Tags);
            stopwatchProvider.CreateAndStore(MeasuredOperation.OpenSelectTagsView).Start();

            var currentTags = tagIds.OrderBy(CommonFunctions.Identity).ToArray();

            var chosenTags = await Navigate<SelectTagsViewModel, SelectTagsParameter, long[]>(new SelectTagsParameter(currentTags, workspaceId));

            if (chosenTags.OrderBy(CommonFunctions.Identity).SequenceEqual(currentTags))
                return;

            var tags = await interactorFactory.GetMultipleTagsById(chosenTags).Execute();

            tagsSubject.OnNext(tags);
        }

        private void toggleBillable()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.Billable);

            isBillableSubject.OnNext(!isBillableSubject.Value);
        }

        private async Task editTimes(EditViewTapSource tapSource)
        {
            analyticsService.EditViewTapped.Track(tapSource);

            var isDurationInitiallyFocused = tapSource == EditViewTapSource.Duration;

            var duration = await durationSubject.FirstAsync();
            var startTime = startTimeSubject.Value;
            var currentDuration = DurationParameter.WithStartAndDuration(startTime, duration);
            var editDurationParam = new EditDurationParameters(currentDuration, false, isDurationInitiallyFocused);

            var selectedDuration = await Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(editDurationParam)
                .ConfigureAwait(false);

            startTimeSubject.OnNext(selectedDuration.Start);
            if (selectedDuration.Duration.HasValue)
                durationSubject.OnNext(selectedDuration.Duration);
        }

        private async Task selectStartDate()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.StartDate);

            var startTime = startTimeSubject.Value;
            var parameters = isRunning
                ? DateTimePickerParameters.ForStartDateOfRunningTimeEntry(startTime, timeService.CurrentDateTime)
                : DateTimePickerParameters.ForStartDateOfStoppedTimeEntry(startTime);

            var selectedStartTime = await Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(parameters)
                .ConfigureAwait(false);

            startTimeSubject.OnNext(selectedStartTime);
        }

        private void stopTimeEntry()
        {
            var duration = timeService.CurrentDateTime - startTimeSubject.Value;
            durationSubject.OnNext(duration);
        }

        private void dismissSyncErrorMessage()
        {
            syncErrorMessageSubject.OnNext(null);
        }

        private async Task<bool> closeWithConfirmation()
        {
            if (await isDirty())
            {
                var userConfirmedDiscardingChanges = await View.ConfirmDestructiveAction(ActionType.DiscardEditingChanges);

                if (!userConfirmedDiscardingChanges)
                    return false;
            }

            await Finish();
            return true;
        }

        private async Task<bool> isDirty()
        {
            var duration = await durationSubject.FirstAsync();
            return originalTimeEntry == null
                || originalTimeEntry.Description != Description.Value
                || originalTimeEntry.WorkspaceId != workspaceId
                || originalTimeEntry.ProjectId != projectId
                || originalTimeEntry.TaskId != taskId
                || originalTimeEntry.Start != startTimeSubject.Value
                || !originalTimeEntry.TagIds.SetEquals(tagIds)
                || originalTimeEntry.Billable != isBillableSubject.Value
                || originalTimeEntry.Duration != (long?)duration?.TotalSeconds;
        }

        private async Task save()
        {
            OnboardingStorage.EditedTimeEntry();

            var timeEntries = await interactorFactory.GetMultipleTimeEntriesById(TimeEntryIds).Execute();

            var duration = await durationSubject.FirstAsync();
            var commonTimeEntryData = new EditTimeEntryDto
            {
                Id = TimeEntryIds.First(),
                Description = Description.Value?.Trim() ?? string.Empty,
                StartTime = startTimeSubject.Value,
                StopTime = calculateStopTime(startTimeSubject.Value, duration),
                ProjectId = projectId,
                TaskId = taskId,
                Billable = isBillableSubject.Value,
                WorkspaceId = workspaceId,
                TagIds = tagIds.ToArray()
            };

            var timeEntriesDtos = timeEntries
                .Select(timeEntry => applyDataFromTimeEntry(commonTimeEntryData, timeEntry))
                .ToArray();

            interactorFactory
                .UpdateMultipleTimeEntries(timeEntriesDtos)
                .Execute()
                .ObserveOn(schedulerProvider.MainScheduler)
                .SubscribeToErrorsAndCompletion((Exception ex) => Finish(), () => Finish())
                .DisposedBy(disposeBag);
        }

        private EditTimeEntryDto applyDataFromTimeEntry(EditTimeEntryDto commonTimeEntryData, IThreadSafeTimeEntry timeEntry)
        {
            commonTimeEntryData.Id = timeEntry.Id;

            if (IsEditingGroup)
            {
                // start and end times can't be changed when editing a group of time entries
                commonTimeEntryData.StartTime = timeEntry.Start;
                commonTimeEntryData.StopTime = calculateStopTime(timeEntry.Start, timeEntry.TimeSpanDuration());
            }

            return commonTimeEntryData;
        }

        private async Task delete()
        {
            var actionType = IsEditingGroup
                ? ActionType.DeleteMultipleExistingTimeEntries
                : ActionType.DeleteExistingTimeEntry;

            var interactor = IsEditingGroup
                ? interactorFactory.DeleteMultipleTimeEntries(TimeEntryIds)
                : interactorFactory.DeleteTimeEntry(TimeEntryId);

            var isDeletionConfirmed = await delete(actionType, TimeEntryIds.Length, interactor);

            if (isDeletionConfirmed)
                await Finish();
        }

        private async Task<bool> delete(ActionType actionType, int entriesCount, IInteractor<IObservable<Unit>> deletionInteractor)
        {
            var isDeletionConfirmed = await View.ConfirmDestructiveAction(actionType, entriesCount);

            if (!isDeletionConfirmed)
                return false;

            await deletionInteractor.Execute();

            syncManager.InitiatePushSync();
            analyticsService.DeleteTimeEntry.Track();

            return true;
        }

        public struct ProjectClientTaskInfo
        {
            public ProjectClientTaskInfo(string project, string projectColor, string client, string task)
            {
                Project = string.IsNullOrEmpty(project) ? null : project;
                ProjectColor = string.IsNullOrEmpty(projectColor) ? null : projectColor;
                Client = string.IsNullOrEmpty(client) ? null : client;
                Task = string.IsNullOrEmpty(task) ? null : task;
            }

            public string Project { get; private set; }
            public string ProjectColor { get; private set; }
            public string Client { get; private set; }
            public string Task { get; private set; }

            public bool HasProject => !string.IsNullOrEmpty(Project);

            public static ProjectClientTaskInfo Empty
                => new ProjectClientTaskInfo(null, null, null, null);
        }
    }
}
