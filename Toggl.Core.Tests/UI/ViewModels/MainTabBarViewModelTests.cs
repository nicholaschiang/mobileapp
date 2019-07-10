using FluentAssertions;
using NSubstitute;
using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Core.Suggestions;
using Toggl.Core.Tests.Generators;
using Toggl.Core.UI.ViewModels;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public class MainTabViewModelTests
    {
        public abstract class MainTabViewModelTest : BaseViewModelTests<MainTabBarViewModel>
        {
            protected override MainTabBarViewModel CreateViewModel()
                => new MainTabBarViewModel(
                    TimeService,
                    DataSource,
                    SyncManager,
                    RatingService,
                    UserPreferences,
                    AnalyticsService,
                    BackgroundService,
                    InteractorFactory,
                    OnboardingStorage,
                    SchedulerProvider,
                    PermissionsChecker,
                    NavigationService,
                    RemoteConfigService,
                    UpdateRemoteConfigCacheService,
                    AccessRestrictionStorage,
                    StopwatchProvider,
                    RxActionFactory,
                    UserAccessManager,
                    PrivateSharedStorageService,
                    PlatformInfo
                );
        }

        public sealed class TheConstructor : MainTabViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                    bool useTimeService,
                    bool useDataSource,
                    bool useSyncManager,
                    bool useRatingService,
                    bool useUserPreferences,
                    bool useAnalyticsService,
                    bool useBackgroundService,
                    bool useInteractorFactory,
                    bool useOnboardingStorage,
                    bool useSchedulerProvider,
                    bool usePermissionsChecker,
                    bool useNavigationService,
                    bool useRemoteConfigService,
                    bool useRemoteConfigUpdateService,
                    bool useAccessRestrictionStorage,
                    bool useStopwatchProvider,
                    bool useRxActionFactory,
                    bool useUserAccessManager,
                    bool usePrivateSharedStorageService,
                    bool usePlatformInfo)
            {
                var timeService = useTimeService ? TimeService : null;
                var dataSource = useDataSource ? DataSource : null;
                var syncManager = useSyncManager ? SyncManager : null;
                var ratingService = useRatingService ? RatingService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var backgroundService = useBackgroundService ? BackgroundService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var permissionsService = usePermissionsChecker ? PermissionsChecker : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var remoteConfigService = useRemoteConfigService ? RemoteConfigService : null;
                var remoteConfigUpdateService = useRemoteConfigUpdateService ? UpdateRemoteConfigCacheService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var privateSharedStorageService = usePrivateSharedStorageService ? PrivateSharedStorageService : null;
                var platformInfo = usePlatformInfo ? PlatformInfo : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MainTabBarViewModel(
                        timeService,
                        dataSource,
                        syncManager,
                        ratingService,
                        userPreferences,
                        analyticsService,
                        backgroundService,
                        interactorFactory,
                        onboardingStorage,
                        schedulerProvider,
                        permissionsService,
                        navigationService,
                        remoteConfigService,
                        remoteConfigUpdateService,
                        accessRestrictionStorage,
                        stopwatchProvider,
                        rxActionFactory,
                        userAccessManager,
                        privateSharedStorageService,
                        platformInfo
                    );

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheTabsProperty : MainTabViewModelTest
        {
            [Theory]
            [InlineData(Platform.Daneel, false)]
            [InlineData(Platform.Giskard, true)]
            public void ShouldContainTheSettingsViewModelBasedOntheRemoteConfigService(Platform platform, bool includesSettings)
            {
                PlatformInfo.Platform.Returns(platform);

                var viewModel = CreateViewModel();

                var expectedTabCount = 3 + (includesSettings ? 1 : 0);

                viewModel.Tabs.Count().Should().Be(expectedTabCount);
            }
        }
    }
}
