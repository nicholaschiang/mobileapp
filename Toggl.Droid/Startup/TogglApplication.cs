using System;
using Android.App;
using Android.Runtime;
using Android.Views.Accessibility;
using Toggl.Core.UI;
using Toggl.Droid.BroadcastReceivers;

namespace Toggl.Droid
{
    [Application(AllowBackup = false)]
    public class TogglApplication : Application
    {
        public TimezoneChangedBroadcastReceiver TimezoneChangedBroadcastReceiver { get; set; }

        public ApplicationLifecycleObserver ApplicationLifecycleObserver { get; set; }

        public TogglApplication(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Firebase.FirebaseApp.InitializeApp(this);

            AndroidDependencyContainer.EnsureInitialized(Context);
            var app = new AppStart(AndroidDependencyContainer.Instance);
            var accessLevel = app.GetAccessLevel();
            app.SetupBackgroundSync();
            app.SetFirstOpened();
            if (accessLevel == AccessLevel.TokenRevoked || accessLevel == AccessLevel.LoggedIn)
            {
                AndroidDependencyContainer.Instance
                    .UserAccessManager
                    .LoginWithSavedCredentials();
            }

            var accessibilityManager = GetSystemService(AccessibilityService) as AccessibilityManager;
            if (accessibilityManager != null)
            {
                var accessibilityEnabled = accessibilityManager.IsTouchExplorationEnabled;
                AndroidDependencyContainer.Instance.AnalyticsService.AccessibilityEnabled.Track(accessibilityEnabled);
            }

#if USE_APPCENTER
            Microsoft.AppCenter.AppCenter.Start(
                "{TOGGL_APP_CENTER_ID_DROID}",
                typeof(Microsoft.AppCenter.Crashes.Crashes),
                typeof(Microsoft.AppCenter.Analytics.Analytics));
#endif

#if DEBUG
            // Add or remove `Detect*` chains to detect unwanted behaviour
            // Change the `Penalty*` to change how the StrictMode works, allowing it to crash the app if necessary
            // Try not to misinterpret the logs/penalties; You should only be looking for behaviour that shouldn't
            // be happening
            Android.OS.StrictMode.SetVmPolicy(
                new Android.OS.StrictMode.VmPolicy.Builder()
                    .DetectActivityLeaks()
                    .DetectLeakedClosableObjects()
                    .DetectLeakedRegistrationObjects()
                    .DetectLeakedSqlLiteObjects()
                    .PenaltyLog()
                    .Build());
            Android.OS.StrictMode.SetThreadPolicy(
                new Android.OS.StrictMode.ThreadPolicy.Builder()
                    .DetectCustomSlowCalls()
                    .PenaltyLog()
                    .Build());
#endif
        }
    }
}
