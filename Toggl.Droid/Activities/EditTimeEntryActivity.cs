using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Text;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Core.Analytics;
using Toggl.Core.Extensions;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Transformations;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.ViewHolders;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using static Toggl.Droid.Resource.String;
using TagsAdapter = Toggl.Droid.Adapters.SimpleAdapter<string>;
using TextResources = Toggl.Shared.Resources;
using TimeEntryExtensions = Toggl.Droid.Extensions.TimeEntryExtensions;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class EditTimeEntryActivity : ReactiveActivity<EditTimeEntryViewModel>
    {
        private TagsAdapter tagsAdapter = new TagsAdapter(Resource.Layout.EditTimeEntryTagCell, StringViewHolder.Create);

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light_WhiteBackground);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.EditTimeEntryActivity);
            restoreTimeEntryIds(bundle);

            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();
            setupViews();
            setupBindings();
        }

        private void restoreTimeEntryIds(Bundle bundle)
        {
            if (bundle == null) return;
            if (!bundle.ContainsKey(nameof(ViewModel.TimeEntryIds))) return;

            var viewModelTimeEntryIds = bundle.GetLongArray(nameof(ViewModel.TimeEntryIds));
            if (viewModelTimeEntryIds == null) return;

            ViewModel.TimeEntryIds = viewModelTimeEntryIds;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState?.PutLongArray(nameof(ViewModel.TimeEntryIds), ViewModel.TimeEntryIds);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            resetOnboardingOnResume();
        }

        protected override void OnStop()
        {
            base.OnStop();
            clearOnboardingOnStop();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        private void setupViews()
        {
            singleTimeEntryModeViews.Visibility = (!ViewModel.IsEditingGroup).ToVisibility();
            timeEntriesGroupModeViews.Visibility = ViewModel.IsEditingGroup.ToVisibility();

            descriptionEditText.Text = ViewModel.Description.Value;

            groupCountTextView.Text = string.Format(
                TextResources.EditingTimeEntryGroup,
                ViewModel.GroupCount);

            var layoutManager = new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 5;
            tagsRecycler.SetLayoutManager(layoutManager);
            tagsRecycler.SetAdapter(tagsAdapter);

            deleteLabel.Text = ViewModel.IsEditingGroup
                ? string.Format(TextResources.DeleteNTimeEntries, ViewModel.GroupCount)
                : TextResources.DeleteThisEntry;

            scrollView.AttachMaterialScrollBehaviour(appBarLayout);
        }

        private void setupBindings()
        {
            closeButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            confirmButton.Rx().Tap()
                .Select(_ => descriptionEditText.HasFocus)
                .Subscribe(handleConfirmClick)
                .DisposedBy(DisposeBag);

            descriptionEditText.Rx().FocusChanged()
                .Select(isFocused => isFocused ? Done : Save)
                .Select(Resources.GetString)
                .Subscribe(confirmButton.Rx().TextObserver());

            descriptionEditText.Rx().Text()
                .Subscribe(ViewModel.Description.Accept)
                .DisposedBy(DisposeBag);

            errorContainer.Rx().Tap()
                .Subscribe(ViewModel.DismissSyncErrorMessage.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.SyncErrorMessage
                .Subscribe(errorText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsSyncErrorMessageVisible
                .Subscribe(errorContainer.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Preferences
                .Select(preferences => preferences.DurationFormat)
                .Select(format => ViewModel.GroupDuration.ToFormattedString(format))
                .Subscribe(groupDurationTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.IsInaccessible
               .Subscribe(adjustUIForInaccessibleTimeEntry)
               .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(generateProjectTaskClientFormattedString)
                .Subscribe(projectTaskClientTextView.Rx().TextFormattedObserver())
                .DisposedBy(DisposeBag);

            projectButton.Rx().Tap()
                .Subscribe(ViewModel.SelectProject.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Tags
                .Select(tags => tags.ToArray())
                .Subscribe(tagsAdapter.Rx().Items())
                .DisposedBy(DisposeBag);

            tagsButton.Rx().Tap()
                .Subscribe(ViewModel.SelectTags.Inputs)
                .DisposedBy(DisposeBag);

            Observable.CombineLatest(
                    tagsAdapter.ItemTapObservable, ViewModel.IsInaccessible,
                    (tap, isInaccessible) => isInaccessible)
                .Where(isInacessible => !isInacessible)
                .SelectUnit()
                .Subscribe(ViewModel.SelectTags.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsBillableAvailable
                .Subscribe(billableRelatedViews.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsBillable
                .Subscribe(billableSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            billableButton.Rx().Tap()
                .Subscribe(ViewModel.ToggleBillable.Inputs)
                .DisposedBy(DisposeBag);

            billableSwitch.Rx().Checked()
                .SelectUnit()
                .Subscribe(ViewModel.ToggleBillable.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(startTime, preferences.TimeOfDayFormat.Format, AndroidDependencyContainer.Instance.AnalyticsService))
                .Subscribe(startTimeTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(startTime, preferences.DateFormat.Short, AndroidDependencyContainer.Instance.AnalyticsService))
                .Subscribe(startDateTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            changeStartTimeButton.Rx().Tap()
               .SelectValue(EditViewTapSource.StartTime)
               .Subscribe(ViewModel.EditTimes.Inputs)
               .DisposedBy(DisposeBag);

            var stopTimeObservable = ViewModel.StopTime
                .Where(stopTime => stopTime.HasValue)
                .Select(stopTime => stopTime.Value);

            stopTimeObservable
                .WithLatestFrom(ViewModel.Preferences,
                    (stopTime, preferences) => DateTimeToFormattedString.Convert(stopTime, preferences.TimeOfDayFormat.Format, AndroidDependencyContainer.Instance.AnalyticsService))
                .Subscribe(stopTimeTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            stopTimeObservable
                .WithLatestFrom(ViewModel.Preferences,
                    (stopTime, preferences) => DateTimeToFormattedString.Convert(stopTime, preferences.DateFormat.Short, AndroidDependencyContainer.Instance.AnalyticsService))
                .Subscribe(stopDateTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            changeStopTimeButton.Rx().Tap()
                .WithLatestFrom(ViewModel.IsTimeEntryRunning, (_, isTimeEntryRunning) => !isTimeEntryRunning)
                .Where(CommonFunctions.Identity)
                .SelectValue(EditViewTapSource.StopTime)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            changeStopTimeButton.Rx().Tap()
                .WithLatestFrom(ViewModel.IsTimeEntryRunning, (_, isTimeEntryRunning) => isTimeEntryRunning)
                .Where(CommonFunctions.Identity)
                .SelectUnit()
                .Subscribe(ViewModel.StopTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Subscribe(stopTimeEntryButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Select(isRunning => !isRunning && !ViewModel.IsEditingGroup)
                .Subscribe(stoppedTimeEntryStopTimeElements.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Duration
                .WithLatestFrom(ViewModel.Preferences,
                    (duration, preferences) => duration.ToFormattedString(preferences.DurationFormat))
                .Subscribe(durationTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            changeDurationButton.Rx().Tap()
                .SelectValue(EditViewTapSource.Duration)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            deleteButton.Rx().Tap()
                .Subscribe(ViewModel.Delete.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void adjustUIForInaccessibleTimeEntry(bool isInaccessible)
        {
            descriptionEditText.Enabled = !isInaccessible;

            changeStartTimeButton.Enabled = !isInaccessible;
            changeStopTimeButton.Enabled = !isInaccessible;
            changeDurationButton.Enabled = !isInaccessible;
            stopTimeEntryButton.Enabled = !isInaccessible;

            billableButton.Enabled = !isInaccessible;
            billableSwitch.Enabled = !isInaccessible;

            tagsButton.Enabled = !isInaccessible;
        }

        private void handleConfirmClick(bool hasFocus)
        {
            if (hasFocus)
            {
                descriptionEditText.RemoveFocus();
            }
            else
            {
                ViewModel.Save.Execute();
            }
        }

        private ISpannable generateProjectTaskClientFormattedString(EditTimeEntryViewModel.ProjectClientTaskInfo projectClientTask)
            => TimeEntryExtensions.ToProjectTaskClient(
                    projectClientTask.HasProject,
                    projectClientTask.Project,
                    projectClientTask.ProjectColor,
                    projectClientTask.Task,
                    projectClientTask.Client);
    }
}
