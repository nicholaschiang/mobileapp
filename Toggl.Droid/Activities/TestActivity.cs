using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.OS;
using Android.Support.V7.App;
using Toggl.Droid.Helper;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/TestTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class TestActivity : AppCompatActivity
    {
        protected sealed override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.TestActivity);

            if (QApis.AreAvailable)
            {
                var uiOptions = SystemUiFlags.HideNavigation | SystemUiFlags.LayoutStable;
//                                | (int)SystemUiFlags.LightStatusBar
//                                | (int)SystemUiFlags.LightNavigationBar;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(int)uiOptions;
            }
        }

    }
}
