using Toggl.Core.Analytics;
using Toggl.Core.Diagnostics;
using Toggl.Core.Interactors;
using Toggl.Core.Login;
using Toggl.Core.UI.Services;
using Toggl.Core.Services;
using Toggl.Core.Shortcuts;
using Toggl.Core.Suggestions;
using Toggl.Core.Sync;
using Toggl.Shared;
using Toggl.Storage;
using Toggl.Storage.Settings;
using Toggl.Networking;
using Toggl.Networking.Network;
using Toggl.Core.UI.Navigation;

namespace Toggl.Core.UI
{
    public class TestDependencyContainer : UIDependencyContainer
    {
        public static void Initialize(TestDependencyContainer container)
        {
            Instance = container;
        }

        public TestDependencyContainer()
            : base(ApiEnvironment.Staging, new UserAgent("Giskard", "999.99"))
        {
        }

        internal IUserAccessManager MockUserAccessManager { get; set; }
        public override IUserAccessManager UserAccessManager
            => MockUserAccessManager ?? base.UserAccessManager;

        internal IAccessRestrictionStorage MockAccessRestrictionStorage { get; set; }
        protected override IAccessRestrictionStorage CreateAccessRestrictionStorage()
            => MockAccessRestrictionStorage;

        internal IAnalyticsService MockAnalyticsService { get; set; }
        protected override IAnalyticsService CreateAnalyticsService()
            => MockAnalyticsService;

        internal IBackgroundSyncService MockBackgroundSyncService { get; set; }
        protected override IBackgroundSyncService CreateBackgroundSyncService()
            => MockBackgroundSyncService;

        internal IBrowserService MockBrowserService { get; set; }
        protected override IBrowserService CreateBrowserService()
            => MockBrowserService;

        internal ICalendarService MockCalendarService { get; set; }
        protected override ICalendarService CreateCalendarService()
            => MockCalendarService;

        internal ITogglDatabase MockDatabase { get; set; }
        protected override ITogglDatabase CreateDatabase()
            => MockDatabase;

        internal IKeyValueStorage MockKeyValueStorage { get; set; }
        protected override IKeyValueStorage CreateKeyValueStorage()
            => MockKeyValueStorage;

        internal ILastTimeUsageStorage MockLastTimeUsageStorage { get; set; }
        protected override ILastTimeUsageStorage CreateLastTimeUsageStorage()
            => MockLastTimeUsageStorage;

        internal ILicenseProvider MockLicenseProvider { get; set; }
        protected override ILicenseProvider CreateLicenseProvider()
            => MockLicenseProvider;

        internal INavigationService MockNavigationService { get; set; }
        protected override INavigationService CreateNavigationService()
            => MockNavigationService;

        internal INotificationService MockNotificationService { get; set; }
        protected override INotificationService CreateNotificationService()
            => MockNotificationService;

        internal IOnboardingStorage MockOnboardingStorage { get; set; }
        protected override IOnboardingStorage CreateOnboardingStorage()
            => MockOnboardingStorage;

        internal IPermissionsChecker MockPermissionsChecker { get; set; }
        protected override IPermissionsChecker CreatePermissionsChecker()
            => MockPermissionsChecker;

        internal IPlatformInfo MockPlatformInfo { get; set; }
        protected override IPlatformInfo CreatePlatformInfo()
            => MockPlatformInfo;

        internal IPrivateSharedStorageService MockPrivateSharedStorageService { get; set; }
        protected override IPrivateSharedStorageService CreatePrivateSharedStorageService()
            => MockPrivateSharedStorageService;

        internal IRatingService MockRatingService { get; set; }
        protected override IRatingService CreateRatingService()
            => MockRatingService;

        internal IRemoteConfigService MockRemoteConfigService { get; set; }
        protected override IRemoteConfigService CreateRemoteConfigService()
            => MockRemoteConfigService;

        internal ISchedulerProvider MockSchedulerProvider { get; set; }
        protected override ISchedulerProvider CreateSchedulerProvider()
            => MockSchedulerProvider;

        internal IApplicationShortcutCreator MockShortcutCreator { get; set; }
        protected override IApplicationShortcutCreator CreateShortcutCreator()
            => MockShortcutCreator;

        internal IStopwatchProvider MockStopwatchProvider { get; set; }
        protected override IStopwatchProvider CreateStopwatchProvider()
            => MockStopwatchProvider;

        internal ISuggestionProviderContainer MockSuggestionProviderContainer { get; set; }
        protected override ISuggestionProviderContainer CreateSuggestionProviderContainer()
            => MockSuggestionProviderContainer;

        internal IUserPreferences MockUserPreferences { get; set; }
        protected override IUserPreferences CreateUserPreferences()
            => MockUserPreferences;

        internal IInteractorFactory MockInteractorFactory { get; set; }
        protected override IInteractorFactory CreateInteractorFactory()
            => MockInteractorFactory;

        internal ITimeService MockTimeService { get; set; }
        protected override ITimeService CreateTimeService()
            => MockTimeService;

        internal ISyncManager MockSyncManager { get; set; }
        protected override ISyncManager CreateSyncManager()
            => MockSyncManager;
    }
}
