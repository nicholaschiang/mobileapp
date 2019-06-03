using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS
{
    public partial class AppDelegate
    {
        private CompositeDisposable lastUpdateDateDisposable = new CompositeDisposable();

        public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
        {
            var interactorFactory = IosDependencyContainer.Instance.InteractorFactory;
            interactorFactory?.RunBackgroundSync()
                .Execute()
                .Select(mapToNativeOutcomes)
                .Subscribe(completionHandler);
        }

        public override void OnActivated(UIApplication application)
        {
            observeAndStoreProperties();
        }

        public override void WillEnterForeground(UIApplication application)
        {
            IosDependencyContainer.Instance.BackgroundService.EnterForeground();
        }

        public override void DidEnterBackground(UIApplication application)
        {
            IosDependencyContainer.Instance.BackgroundService.EnterBackground();
        }

        private UIBackgroundFetchResult mapToNativeOutcomes(Core.Models.SyncOutcome outcome)
        {
            switch (outcome)
            {
                case Core.Models.SyncOutcome.NewData:
                    return UIBackgroundFetchResult.NewData;
                case Core.Models.SyncOutcome.NoData:
                    return UIBackgroundFetchResult.NoData;
                case Core.Models.SyncOutcome.Failed:
                    return UIBackgroundFetchResult.Failed;
                default:
                    return UIBackgroundFetchResult.Failed;
            }
        }

        private void observeAndStoreProperties()
        {
            lastUpdateDateDisposable.Dispose();
            lastUpdateDateDisposable = new CompositeDisposable();

            try
            {
                var interactorFactory = IosDependencyContainer.Instance.InteractorFactory;
                var privateSharedStorage = IosDependencyContainer.Instance.PrivateSharedStorageService;

                interactorFactory.ObserveDefaultWorkspaceId().Execute()
                    .Subscribe(privateSharedStorage.SaveDefaultWorkspaceId)
                    .DisposedBy(lastUpdateDateDisposable);
            }
            catch (Exception)
            {
                // Ignore errors when logged out
            }
        }
    }
}
