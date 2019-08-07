using NSubstitute;
using System.Reactive.Concurrency;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Services;
using Toggl.Core.Sync;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.Services;
using Toggl.Networking;
using Toggl.Storage;
using Toggl.Storage.Settings;

namespace Toggl.Core.Tests.Sync.Helpers
{
    public sealed class AppServices
    {
        private readonly ISyncErrorHandlingService syncErrorHandlingService;

        public IScheduler Scheduler { get; }

        public ITimeService TimeService { get; }

        public IAccessRestrictionStorage AccessRestrictionStorageSubsitute { get; } =
            Substitute.For<IAccessRestrictionStorage>();

        public INavigationService NavigationServiceSubstitute { get; } =
            Substitute.For<INavigationService>();

        public IAnalyticsService AnalyticsServiceSubstitute { get; } = Substitute.For<IAnalyticsService>();

        public ILastTimeUsageStorage LastTimeUsageStorageSubstitute { get; } = Substitute.For<ILastTimeUsageStorage>();

        public ISyncManager SyncManager { get; }

        public IAutomaticSyncingService AutomaticSyncingService { get; } = Substitute.For<IAutomaticSyncingService>();

        public AppServices(ITogglApi api, ITogglDatabase database)
        {
            Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            TimeService = new TimeService(Scheduler);

            var errorHandlingService = new ErrorHandlingService(NavigationServiceSubstitute, AccessRestrictionStorageSubsitute);
            syncErrorHandlingService = new SyncErrorHandlingService(errorHandlingService);

            var dataSource = new TogglDataSource(
                database,
                TimeService,
                AnalyticsServiceSubstitute);

            SyncManager = TogglSyncManager.CreateSyncManager(
                database,
                api,
                dataSource,
                TimeService,
                AnalyticsServiceSubstitute,
                LastTimeUsageStorageSubstitute,
                Scheduler,
                AutomaticSyncingService);

            syncErrorHandlingService.HandleErrorsOf(SyncManager);
        }
    }
}
