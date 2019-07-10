using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Diagnostics;
using Toggl.Core.DTOs;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Login;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Services;
using Toggl.Core.Sync;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.Services;
using Toggl.Core.UI.Transformations;
using Toggl.Core.UI.ViewModels.Settings;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage.Settings;
using Xamarin.Essentials;
using static Toggl.Shared.Extensions.CommonFunctions;

namespace Toggl.Core.UI.ViewModels
{
    using WorkspaceToSelectableWorkspaceLambda = Func<IEnumerable<IThreadSafeWorkspace>, IList<SelectableWorkspaceViewModel>>;

    [Preserve(AllMembers = true)]
    public sealed class SettingsViewModel : ViewModel
    {
        private readonly ISubject<Unit> loggingOutSubject = new Subject<Unit>();
        private readonly ISubject<bool> isFeedbackSuccessViewShowing = new Subject<bool>();
        private readonly ISubject<bool> calendarPermissionGranted = new BehaviorSubject<bool>(false);
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITogglDataSource dataSource;
        private readonly ISyncManager syncManager;
        private readonly IUserAccessManager userAccessManager;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IPlatformInfo platformInfo;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IRxActionFactory rxActionFactory;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IPermissionsChecker permissionsChecker;

        private bool isSyncing;
        private bool isLoggingOut;
        private IThreadSafeUser currentUser;
        private IThreadSafePreferences currentPreferences;
        private IStopwatch navigationFromMainViewModelStopwatch;

        public string Title { get; private set; } = Resources.Settings;
        public bool CalendarSettingsEnabled => onboardingStorage.CompletedCalendarOnboarding();
        public string Version => $"{platformInfo.Version} ({platformInfo.BuildNumber})";

        public IObservable<string> Name { get; }
        public IObservable<string> Email { get; }
        public IObservable<bool> IsSynced { get; }
        public IObservable<Unit> LoggingOut { get; }
        public IObservable<string> DateFormat { get; }
        public IObservable<bool> IsRunningSync { get; }
        public IObservable<string> WorkspaceName { get; }
        public IObservable<string> DurationFormat { get; }
        public IObservable<string> BeginningOfWeek { get; }
        public IObservable<bool> IsManualModeEnabled { get; }
        public IObservable<bool> IsGroupingTimeEntries { get; }
        public IObservable<bool> AreRunningTimerNotificationsEnabled { get; }
        public IObservable<bool> AreStoppedTimerNotificationsEnabled { get; }
        public IObservable<bool> UseTwentyFourHourFormat { get; }
        public IObservable<bool> IsFeedbackSuccessViewShowing { get; }
        public IObservable<bool> IsCalendarSmartRemindersVisible { get; }
        public IObservable<string> CalendarSmartReminders { get; }

        public UIAction OpenCalendarSettings { get; }
        public UIAction OpenCalendarSmartReminders { get; }
        public UIAction OpenNotificationSettings { get; }
        public UIAction ToggleTwentyFourHourSettings { get; }
        public UIAction OpenHelpView { get; }
        public UIAction TryLogout { get; }
        public UIAction OpenAboutView { get; }
        public UIAction OpenSiriShortcuts { get; }
        public UIAction OpenSiriWorkflows { get; }
        public UIAction SubmitFeedback { get; }
        public UIAction SelectDateFormat { get; }
        public UIAction PickDefaultWorkspace { get; }
        public UIAction SelectDurationFormat { get; }
        public UIAction ToggleTimeEntriesGrouping { get; }
        public UIAction SelectBeginningOfWeek { get; }

        public SettingsViewModel(
            ITogglDataSource dataSource,
            ISyncManager syncManager,
            IPlatformInfo platformInfo,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IUserAccessManager userAccessManager,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            INavigationService navigationService,
            IPrivateSharedStorageService privateSharedStorageService,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory rxActionFactory,
            IPermissionsChecker permissionsChecker,
            ISchedulerProvider schedulerProvider)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(permissionsChecker, nameof(permissionsChecker));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.dataSource = dataSource;
            this.syncManager = syncManager;
            this.platformInfo = platformInfo;
            this.userPreferences = userPreferences;
            this.rxActionFactory = rxActionFactory;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;
            this.userAccessManager = userAccessManager;
            this.onboardingStorage = onboardingStorage;
            this.stopwatchProvider = stopwatchProvider;
            this.schedulerProvider = schedulerProvider;
            this.permissionsChecker = permissionsChecker;
            this.privateSharedStorageService = privateSharedStorageService;

            IsSynced =
                syncManager.ProgressObservable
                    .SelectMany(checkSynced)
                    .AsDriver(schedulerProvider);

            IsRunningSync =
                syncManager.ProgressObservable
                    .Select(isRunningSync)
                    .AsDriver(schedulerProvider);

            Name =
                dataSource.User.Current
                    .Select(user => user.Fullname)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            Email =
                dataSource.User.Current
                    .Select(user => user.Email.ToString())
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            IsManualModeEnabled = userPreferences.IsManualModeEnabledObservable
                .AsDriver(schedulerProvider);

            AreRunningTimerNotificationsEnabled = userPreferences.AreRunningTimerNotificationsEnabledObservable
                .AsDriver(schedulerProvider);

            AreStoppedTimerNotificationsEnabled = userPreferences.AreStoppedTimerNotificationsEnabledObservable
                .AsDriver(schedulerProvider);

            WorkspaceName =
                dataSource.User.Current
                    .DistinctUntilChanged(user => user.DefaultWorkspaceId)
                    .SelectMany(_ => interactorFactory.GetDefaultWorkspace()
                        .TrackException<InvalidOperationException, IThreadSafeWorkspace>("SettingsViewModel.constructor")
                        .Execute()
                    )
                    .Select(workspace => workspace.Name)
                    .AsDriver(schedulerProvider);

            BeginningOfWeek =
                dataSource.User.Current
                    .Select(user => user.BeginningOfWeek)
                    .DistinctUntilChanged()
                    .Select(beginningOfWeek => beginningOfWeek.ToString())
                    .AsDriver(schedulerProvider);

            DateFormat =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.DateFormat.Localized)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            DurationFormat =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.DurationFormat)
                    .Select(DurationFormatToString.Convert)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            UseTwentyFourHourFormat =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.TimeOfDayFormat.IsTwentyFourHoursFormat)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            IsGroupingTimeEntries =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.CollapseTimeEntries)
                    .DistinctUntilChanged()
                    .AsDriver(false, schedulerProvider);

            IsCalendarSmartRemindersVisible = calendarPermissionGranted.AsObservable()
                .CombineLatest(userPreferences.EnabledCalendars.Select(ids => ids.Any()), CommonFunctions.And);

            CalendarSmartReminders = userPreferences.CalendarNotificationsSettings()
                .Select(s => s.Title())
                .DistinctUntilChanged();

            LoggingOut = loggingOutSubject.AsObservable()
                .AsDriver(schedulerProvider);

            dataSource.User.Current
                .Subscribe(user => currentUser = user)
                .DisposedBy(disposeBag);

            dataSource.Preferences.Current
                .Subscribe(preferences => currentPreferences = preferences)
                .DisposedBy(disposeBag);

            IsRunningSync
                .Subscribe(isSyncing => this.isSyncing = isSyncing)
                .DisposedBy(disposeBag);

            IsFeedbackSuccessViewShowing = isFeedbackSuccessViewShowing.AsObservable()
                .AsDriver(schedulerProvider);

            OpenCalendarSettings = rxActionFactory.FromAsync(openCalendarSettings);
            OpenCalendarSmartReminders = rxActionFactory.FromAsync(openCalendarSmartReminders);
            OpenNotificationSettings = rxActionFactory.FromAsync(openNotificationSettings);
            ToggleTwentyFourHourSettings = rxActionFactory.FromAsync(toggleUseTwentyFourHourClock);
            OpenHelpView = rxActionFactory.FromAsync(openHelpView);
            TryLogout = rxActionFactory.FromAsync(tryLogout);
            OpenAboutView = rxActionFactory.FromAsync(openAboutView);
            OpenSiriShortcuts = rxActionFactory.FromAsync(openSiriShorcuts);
            OpenSiriWorkflows = rxActionFactory.FromAsync(openSiriWorkflows);
            SubmitFeedback = rxActionFactory.FromAsync(submitFeedback);
            SelectDateFormat = rxActionFactory.FromAsync(selectDateFormat);
            PickDefaultWorkspace = rxActionFactory.FromAsync(pickDefaultWorkspace);
            SelectDurationFormat = rxActionFactory.FromAsync(selectDurationFormat);
            SelectBeginningOfWeek = rxActionFactory.FromAsync(selectBeginningOfWeek);
            ToggleTimeEntriesGrouping = rxActionFactory.FromAsync(toggleTimeEntriesGrouping);
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            await checkCalendarPermissions();
            navigationFromMainViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenSettingsView);
            stopwatchProvider.Remove(MeasuredOperation.OpenStartView);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromMainViewModelStopwatch?.Stop();
            navigationFromMainViewModelStopwatch = null;
            checkCalendarPermissions();
        }

        public void CloseFeedbackSuccessView()
        {
            isFeedbackSuccessViewShowing.OnNext(false);
        }

        private async Task toggleUseTwentyFourHourClock()
        {
            var timeFormat = currentPreferences.TimeOfDayFormat.IsTwentyFourHoursFormat
                ? TimeFormat.TwelveHoursFormat
                : TimeFormat.TwentyFourHoursFormat;

            await updatePreferences(timeFormat: timeFormat);
        }

        public void ToggleManualMode()
        {
            if (userPreferences.IsManualModeEnabled)
            {
                userPreferences.EnableTimerMode();
            }
            else
            {
                userPreferences.EnableManualMode();
            }
        }

        public void ToggleRunningTimerNotifications()
        {
            var newState = !userPreferences.AreRunningTimerNotificationsEnabled;
            userPreferences.SetRunningTimerNotifications(newState);
        }

        public void ToggleStoppedTimerNotifications()
        {
            var newState = !userPreferences.AreStoppedTimerNotificationsEnabled;
            userPreferences.SetStoppedTimerNotifications(newState);
        }

        private Task openCalendarSettings()
            => Navigate<CalendarSettingsViewModel, bool, string[]>(false);

        private Task openCalendarSmartReminders()
            => Navigate<UpcomingEventsNotificationSettingsViewModel, Unit>();

        private Task openNotificationSettings()
            => Navigate<NotificationSettingsViewModel>();

        private IObservable<Unit> logout()
        {
            isLoggingOut = true;
            loggingOutSubject.OnNext(Unit.Default);

            return interactorFactory.Logout(LogoutSource.Settings)
                .Execute()
                .Do(_ => Navigate<LoginViewModel, CredentialsParameter>(CredentialsParameter.Empty));
        }

        private IObservable<bool> isSynced()
            => dataSource.HasUnsyncedData().Select(Invert);

        private IObservable<bool> checkSynced(SyncProgress progress)
        {
            if (isLoggingOut || progress != SyncProgress.Synced)
                return Observable.Return(false);

            return isSynced();
        }

        private bool isRunningSync(SyncProgress progress)
            => isLoggingOut == false && progress == SyncProgress.Syncing;

        private async Task updatePreferences(
            New<DurationFormat> durationFormat = default(New<DurationFormat>),
            New<DateFormat> dateFormat = default(New<DateFormat>),
            New<TimeFormat> timeFormat = default(New<TimeFormat>),
            New<bool> collapseTimeEntries = default(New<bool>))
        {
            var preferencesDto = new EditPreferencesDTO
            {
                DurationFormat = durationFormat,
                DateFormat = dateFormat,
                TimeOfDayFormat = timeFormat,
                CollapseTimeEntries = collapseTimeEntries
            };

            await interactorFactory.UpdatePreferences(preferencesDto).Execute();
            syncManager.InitiatePushSync();
        }

        private Task openHelpView() =>
            Browser.OpenAsync(platformInfo.HelpUrl, BrowserLaunchMode.SystemPreferred);

        private async Task tryLogout()
        {
            var synced = !isSyncing && await isSynced();
            if (synced)
            {
                await logout();
                return;
            }

            var (title, message) = isSyncing
                ? (Resources.SettingsSyncInProgressTitle, Resources.SettingsSyncInProgressMessage)
                : (Resources.SettingsUnsyncedTitle, Resources.SettingsUnsyncedMessage);

            await View
                .Confirm(title, message, Resources.SettingsDialogButtonSignOut, Resources.Cancel)
                .SelectMany(shouldLogout
                    => shouldLogout ? logout() : Observable.Return(Unit.Default));
        }

        private Task openAboutView()
            => Navigate<AboutViewModel>();

        private Task openSiriShorcuts()
            => Navigate<SiriShortcutsViewModel>();

        private Task openSiriWorkflows()
            => Navigate<SiriWorkflowsViewModel>();

        private async Task submitFeedback()
        {
            var sendFeedbackSucceed = await Navigate<SendFeedbackViewModel, bool>();
            isFeedbackSuccessViewShowing.OnNext(sendFeedbackSucceed);
        }

        private async Task selectDateFormat()
        {
            var newDateFormat = await Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(currentPreferences.DateFormat);

            if (currentPreferences.DateFormat == newDateFormat)
                return;

            await updatePreferences(dateFormat: newDateFormat);
        }

        private async Task pickDefaultWorkspace()
        {
            var defaultWorkspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("SettingsViewModel.PickDefaultWorkspace")
                .Execute();

            var selectedWorkspaceId =
                await Navigate<SelectWorkspaceViewModel, SelectWorkspaceParameters, long>(new SelectWorkspaceParameters(Resources.SetDefaultWorkspace, defaultWorkspace.Id));

            if (selectedWorkspaceId == currentUser.DefaultWorkspaceId)
                return;

            await interactorFactory.UpdateDefaultWorkspace(selectedWorkspaceId).Execute();
            syncManager.InitiatePushSync();
        }

        private async Task toggleTimeEntriesGrouping()
        {
            var newValue = !currentPreferences.CollapseTimeEntries;
            analyticsService.GroupTimeEntriesSettingsChanged.Track(newValue);
            await updatePreferences(collapseTimeEntries: newValue);
        }

        private async Task selectDurationFormat()
        {
            var newDurationFormat = await Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(currentPreferences.DurationFormat);

            if (currentPreferences.DurationFormat == newDurationFormat)
                return;

            await updatePreferences(newDurationFormat);
        }

        private async Task selectBeginningOfWeek()
        {
            var newBeginningOfWeek = await Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(currentUser
                    .BeginningOfWeek);

            if (currentUser.BeginningOfWeek == newBeginningOfWeek)
                return;

            await interactorFactory.UpdateUser(new EditUserDTO { BeginningOfWeek = newBeginningOfWeek }).Execute();
            syncManager.InitiatePushSync();
        }

        private async Task checkCalendarPermissions()
        {
            var authorized = permissionsChecker.CalendarPermissionGranted.FirstAsync().GetAwaiter().GetResult();
            calendarPermissionGranted.OnNext(authorized);
        }
    }
}
