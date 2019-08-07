using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System;
using System.Reactive.Linq;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.Presentation;
using Toggl.Shared.Extensions;
using FoundationResources = Toggl.Shared.Resources;

namespace Toggl.Droid.Fragments
{
    public sealed partial class SettingsFragment : ReactiveTabFragment<SettingsViewModel>, IScrollableToTop
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.SettingsFragment, container, false);

            InitializeViews(view);
            setupToolbar();

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            versionTextView.Text = ViewModel.Version;

            ViewModel.Name
                .Subscribe(nameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Email
                .Subscribe(emailTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceName
                .Subscribe(defaultWorkspaceNameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsManualModeEnabled
                .Subscribe(manualModeSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsGroupingTimeEntries
               .Subscribe(groupTimeEntriesSwitch.Rx().CheckedObserver())
               .DisposedBy(DisposeBag);

            ViewModel.UseTwentyFourHourFormat
                .Subscribe(is24hoursModeSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            ViewModel.AreRunningTimerNotificationsEnabled
                .Subscribe(runningTimerNotificationsSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            ViewModel.AreStoppedTimerNotificationsEnabled
                .Subscribe(stoppedTimerNotificationsSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            ViewModel.DateFormat
                .Subscribe(dateFormatTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.BeginningOfWeek
                .Subscribe(beginningOfWeekTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.DurationFormat
                .Subscribe(durationFormatTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsCalendarSmartRemindersVisible
                .Subscribe(smartRemindersView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsCalendarSmartRemindersVisible
                .Subscribe(smartRemindersViewSeparator.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CalendarSmartReminders
                .Subscribe(smartRemindersTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.LoggingOut
                .Subscribe(Context.CancelAllNotifications)
                .DisposedBy(DisposeBag);

            ViewModel.IsFeedbackSuccessViewShowing
                .Subscribe(showFeedbackSuccessToast)
                .DisposedBy(DisposeBag);

            logoutView.Rx()
                .BindAction(ViewModel.TryLogout)
                .DisposedBy(DisposeBag);

            helpView.Rx()
                .BindAction(ViewModel.OpenHelpView)
                .DisposedBy(DisposeBag);

            aboutView.Rx()
                .BindAction(ViewModel.OpenAboutView)
                .DisposedBy(DisposeBag);

            defaultWorkspaceView.Rx()
                .BindAction(ViewModel.PickDefaultWorkspace)
                .DisposedBy(DisposeBag);

            feedbackView.Rx()
                .BindAction(ViewModel.SubmitFeedback)
                .DisposedBy(DisposeBag);

            manualModeView.Rx().Tap()
                .Subscribe(ViewModel.ToggleManualMode)
                .DisposedBy(DisposeBag);

            groupTimeEntriesView.Rx()
                .BindAction(ViewModel.ToggleTimeEntriesGrouping)
                .DisposedBy(DisposeBag);

            is24hoursModeView.Rx()
                .BindAction(ViewModel.ToggleTwentyFourHourSettings)
                .DisposedBy(DisposeBag);

            runningTimerNotificationsView.Rx().Tap()
                .Subscribe(ViewModel.ToggleRunningTimerNotifications)
                .DisposedBy(DisposeBag);

            stoppedTimerNotificationsView.Rx().Tap()
                .Subscribe(ViewModel.ToggleStoppedTimerNotifications)
                .DisposedBy(DisposeBag);

            dateFormatView.Rx().Tap()
                .Subscribe(ViewModel.SelectDateFormat.Inputs)
                .DisposedBy(DisposeBag);

            beginningOfWeekView.Rx()
                .BindAction(ViewModel.SelectBeginningOfWeek)
                .DisposedBy(DisposeBag);

            durationFormatView.Rx().Tap()
                .Subscribe(ViewModel.SelectDurationFormat.Inputs)
                .DisposedBy(DisposeBag);

            calendarSettingsView.Rx().Tap()
                .Subscribe(ViewModel.OpenCalendarSettings.Inputs)
                .DisposedBy(DisposeBag);

            smartRemindersView.Rx().Tap()
                .Subscribe(ViewModel.OpenCalendarSmartReminders.Inputs)
                .DisposedBy(DisposeBag);
        }

        public void ScrollToTop()
        {
            scrollView?.SmoothScrollTo(0, 0);
        }

        private void showFeedbackSuccessToast(bool succeeeded)
        {
            if (!succeeeded) return;

            var toast = Toast.MakeText(Context, Resource.String.SendFeedbackSuccessMessage, ToastLength.Long);
            toast.SetGravity(GravityFlags.CenterHorizontal | GravityFlags.Bottom, 0, 0);
            toast.Show();
        }

        private void setupToolbar()
        {
            var activity = Activity as AppCompatActivity;
            toolbar.Title = FoundationResources.Settings;
            scrollView.AttachMaterialScrollBehaviour(appBarLayout);
            activity.SetSupportActionBar(toolbar);
        }
    }
}
