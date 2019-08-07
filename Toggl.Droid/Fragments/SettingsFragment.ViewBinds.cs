using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Droid.Fragments
{
    public sealed partial class SettingsFragment
    {
        private View helpView;
        private View aboutView;
        private View logoutView;
        private View feedbackView;
        private View manualModeView;
        private View is24hoursModeView;
        private View runningTimerNotificationsView;
        private View stoppedTimerNotificationsView;
        private View dateFormatView;
        private View beginningOfWeekView;
        private View durationFormatView;
        private View calendarSettingsView;
        private View smartRemindersView;
        private View smartRemindersViewSeparator;
        private View groupTimeEntriesView;
        private View defaultWorkspaceView;

        private TextView nameTextView;
        private TextView emailTextView;
        private TextView versionTextView;
        private TextView dateFormatTextView;
        private TextView beginningOfWeekTextView;
        private TextView durationFormatTextView;
        private TextView smartRemindersTextView;
        private TextView defaultWorkspaceNameTextView;

        private Switch is24hoursModeSwitch;
        private Switch manualModeSwitch;
        private Switch runningTimerNotificationsSwitch;
        private Switch stoppedTimerNotificationsSwitch;
        private Switch groupTimeEntriesSwitch;

        private Toolbar toolbar;
        private AppBarLayout appBarLayout;
        private NestedScrollView scrollView;

        protected override void InitializeViews(View fragmentView)
        {
            helpView = fragmentView.FindViewById(Resource.Id.SettingsHelpButton);
            aboutView = fragmentView.FindViewById(Resource.Id.SettingsAboutContainer);
            logoutView = fragmentView.FindViewById(Resource.Id.SettingsLogoutButton);
            feedbackView = fragmentView.FindViewById(Resource.Id.SettingsSubmitFeedbackButton);
            manualModeView = fragmentView.FindViewById(Resource.Id.SettingsToggleManualModeView);
            is24hoursModeView = fragmentView.FindViewById(Resource.Id.SettingsIs24HourModeView);
            dateFormatView = fragmentView.FindViewById(Resource.Id.SettingsDateFormatView);
            beginningOfWeekView = fragmentView.FindViewById(Resource.Id.SettingsSelectBeginningOfWeekView);
            durationFormatView = fragmentView.FindViewById(Resource.Id.SettingsDurationFormatView);
            calendarSettingsView = fragmentView.FindViewById(Resource.Id.CalendarSettingsView);
            smartRemindersView = fragmentView.FindViewById(Resource.Id.SmartRemindersView);
            smartRemindersViewSeparator = fragmentView.FindViewById(Resource.Id.SmartRemindersViewSeparator);
            runningTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsRunningTimerNotificationsView);
            stoppedTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsStoppedTimerNotificationsView);
            groupTimeEntriesView = fragmentView.FindViewById(Resource.Id.GroupTimeEntriesView);
            defaultWorkspaceView = fragmentView.FindViewById(Resource.Id.DefaultWorkspaceView);

            nameTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsNameTextView);
            emailTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsEmailTextView);
            versionTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsAppVersionTextView);
            dateFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDateFormatTextView);
            beginningOfWeekTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsBeginningOfWeekTextView);
            durationFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDurationFormatTextView);
            smartRemindersTextView = fragmentView.FindViewById<TextView>(Resource.Id.SmartRemindersTextView);
            defaultWorkspaceNameTextView = fragmentView.FindViewById<TextView>(Resource.Id.DefaultWorkspaceName);

            manualModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIsManualModeEnabledSwitch);
            is24hoursModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIs24HourModeSwitch);
            runningTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreRunningTimerNotificationsEnabledSwitch);
            stoppedTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreStoppedTimerNotificationsEnabledSwitch);
            groupTimeEntriesSwitch = fragmentView.FindViewById<Switch>(Resource.Id.GroupTimeEntriesSwitch);

            toolbar = fragmentView.FindViewById<Toolbar>(Resource.Id.Toolbar);
            appBarLayout = fragmentView.FindViewById<AppBarLayout>(Resource.Id.AppBarLayout);

            scrollView = fragmentView.FindViewById<NestedScrollView>(Resource.Id.ScrollView);
        }
    }
}
