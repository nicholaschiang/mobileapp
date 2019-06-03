using System.Reactive;
using Foundation;
using Toggl.Core;
using Toggl.Core.UI;
using Toggl.Core.UI.Navigation;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Presentation;
using Toggl.Networking;
using UIKit;
using UserNotifications;

namespace Toggl.iOS
{
    [Register(nameof(AppDelegate))]
    public sealed partial class AppDelegate : UIApplicationDelegate, IUNUserNotificationCenterDelegate
    {
        private const ApiEnvironment environment =
        #if USE_PRODUCTION_API
            ApiEnvironment.Production;
        #else
            ApiEnvironment.Staging;
        #endif

        private CompositePresenter compositePresenter;
        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            #if !USE_PRODUCTION_API
            System.Net.ServicePointManager.ServerCertificateValidationCallback
                  += (sender, certificate, chain, sslPolicyErrors) => true;
            #endif

            initializeAnalytics();

            var version = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();

            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            Window.MakeKeyAndVisible();

            setupNavigationBar();
            setupTabBar();

            compositePresenter = new CompositePresenter(
                new RootPresenter(Window, this),
                new NavigationPresenter(Window, this),
                new ModalDialogPresenter(Window, this),
                new ModalCardPresenter(Window, this)
            );

            IosDependencyContainer.EnsureInitialized(compositePresenter, environment, Platform.Daneel, version);
            var app = new App<OnboardingViewModel, Unit>(IosDependencyContainer.Instance);
            var hasFullAccess = app.NavigateIfUserDoesNotHaveFullAccess();
            if (hasFullAccess)
            {
                var viewModel = IosDependencyContainer.Instance.ViewModelLoader
                    .Load<Unit, Unit>(typeof(MainTabBarViewModel), Unit.Default).GetAwaiter().GetResult();
                Window.RootViewController = ViewControllerLocator.GetViewController(viewModel);
            }

            UNUserNotificationCenter.Current.Delegate = this;

            #if ENABLE_TEST_CLOUD
            Xamarin.Calabash.Start();
            #endif

            return true;
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (url.Scheme == ApplicationUrls.Scheme)
            {
                handleDeeplink(url);
                return true;
            }

            #if USE_ANALYTICS
            var openUrlOptions = new UIApplicationOpenUrlOptions(options);
            return Google.SignIn.SignIn.SharedInstance.HandleUrl(url, openUrlOptions.SourceApplication, openUrlOptions.Annotation);
            #endif

            return false;
        }

        public override void ReceiveMemoryWarning(UIApplication application)
        {
            IosDependencyContainer.Instance.AnalyticsService.ReceivedLowMemoryWarning.Track(Platform.Daneel);
        }

        public override void ApplicationSignificantTimeChange(UIApplication application)
        {
            IosDependencyContainer.Instance.TimeService.SignificantTimeChanged();
        }
    }
}
