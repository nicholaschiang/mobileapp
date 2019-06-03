using FluentAssertions;
using NSubstitute;
using System;
using System.Linq;
using Toggl.Core.Analytics;
using Toggl.Core.Diagnostics;
using Toggl.Core.Interactors;
using Toggl.Core.Login;
using Toggl.Core.Services;
using Toggl.Core.Shortcuts;
using Toggl.Core.Suggestions;
using Toggl.Core.Sync;
using Toggl.Core.UI;
using Toggl.Core.UI.Services;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.Navigation;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage;
using Toggl.Storage.Settings;
using Xunit;

namespace Toggl.Core.Tests.UI
{
    public class ViewModelLocatorTests : BaseTest
    {
        [Fact, LogIfTooSlow]
        public void IsAbleToCreateEveryViewModel()
        {
            var loader = new ViewModelLoader(new TestDependencyContainer
            {
                MockUserAccessManager = Substitute.For<IUserAccessManager>(),
                MockAccessRestrictionStorage = Substitute.For<IAccessRestrictionStorage>(),
                MockAnalyticsService = Substitute.For<IAnalyticsService>(),
                MockBackgroundSyncService = Substitute.For<IBackgroundSyncService>(),
                MockBrowserService = Substitute.For<IBrowserService>(),
                MockCalendarService = Substitute.For<ICalendarService>(),
                MockDatabase = Substitute.For<ITogglDatabase>(),
                MockKeyValueStorage = Substitute.For<IKeyValueStorage>(),
                MockLastTimeUsageStorage = Substitute.For<ILastTimeUsageStorage>(),
                MockLicenseProvider = Substitute.For<ILicenseProvider>(),
                MockNavigationService = Substitute.For<INavigationService>(),
                MockNotificationService = Substitute.For<INotificationService>(),
                MockOnboardingStorage = Substitute.For<IOnboardingStorage>(),
                MockPermissionsChecker = Substitute.For<IPermissionsChecker>(),
                MockPlatformInfo = Substitute.For<IPlatformInfo>(),
                MockPrivateSharedStorageService = Substitute.For<IPrivateSharedStorageService>(),
                MockRatingService = Substitute.For<IRatingService>(),
                MockRemoteConfigService = Substitute.For<IRemoteConfigService>(),
                MockSchedulerProvider = Substitute.For<ISchedulerProvider>(),
                MockShortcutCreator = Substitute.For<IApplicationShortcutCreator>(),
                MockStopwatchProvider = Substitute.For<IStopwatchProvider>(),
                MockSuggestionProviderContainer = Substitute.For<ISuggestionProviderContainer>(),
                MockUserPreferences = Substitute.For<IUserPreferences>(),
                MockInteractorFactory = Substitute.For<IInteractorFactory>(),
                MockTimeService = Substitute.For<ITimeService>(),
                MockSyncManager = Substitute.For<ISyncManager>(),
            });

            var viewModelTypes = typeof(MainViewModel).Assembly
                .GetTypes()
                .Where(isViewModel);

            var loadMethod = typeof(ViewModelLoader)
                .GetMethod(nameof(ViewModelLoader.Load));

            foreach (var viewModelType in viewModelTypes)
            {
                var typeArguments = getGenericArguments(viewModelType);
                var genericLoadMethod = loadMethod.MakeGenericMethod(typeArguments);

                var arguments = new object[]
                {
                    viewModelType,
                    getDefaultValue(typeArguments.First())
                };

                Action tryingToFindAViewModel = () => genericLoadMethod.Invoke(loader, arguments);
                tryingToFindAViewModel.Should().NotThrow();
            }

            bool isViewModel(Type type)
                => type.IsAbstract == false &&
                   type.Name != nameof(IViewModel) &&
                   type.ImplementsOrDerivesFrom<IViewModel>();

            Type[] getGenericArguments(Type type)
                => type.BaseType.GetGenericArguments().Count() == 2
                ? type.BaseType.GetGenericArguments()
                : getGenericArguments(type.BaseType);

            object getDefaultValue(Type type)
                => type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }
    }
}
