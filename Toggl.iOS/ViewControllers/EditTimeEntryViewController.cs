using CoreGraphics;
using Foundation;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.Analytics;
using Toggl.Core.Extensions;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.Onboarding.EditView;
using Toggl.Core.UI.Transformations;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.Transformations;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;
using Math = System.Math;

namespace Toggl.iOS.ViewControllers
{
    public partial class EditTimeEntryViewController : KeyboardAwareViewController<EditTimeEntryViewModel>
    {
        private const float nonScrollableContentHeight = 116f;
        private const double preferredIpadHeight = 228;

        private IDisposable projectOnboardingDisposable;
        private IDisposable contentSizeChangedDisposable;

        private ProjectTaskClientToAttributedString projectTaskClientToAttributedString;
        private TagsListToAttributedString tagsListToAttributedString;
        private float keyboardHeight = 0;

        private const string boundsKey = "bounds";

        public EditTimeEntryViewController(EditTimeEntryViewModel viewModel)
            : base(viewModel, nameof(EditTimeEntryViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            projectTaskClientToAttributedString = new ProjectTaskClientToAttributedString(
                ProjectTaskClientLabel.Font.CapHeight,
                Colors.EditTimeEntry.ClientText.ToNativeColor());

            tagsListToAttributedString = new TagsListToAttributedString(TagsTextView);

            localizeLabels();
            prepareViews();
            prepareOnboarding();

            contentSizeChangedDisposable = ScrollViewContent.AddObserver(boundsKey, NSKeyValueObservingOptions.New, onContentSizeChanged);

            DescriptionTextView.Text = ViewModel.Description.Value;

            ViewModel.Preferences
                .Select(preferences => preferences.DurationFormat)
                .Select(format => ViewModel.GroupDuration.ToFormattedString(format))
                .Subscribe(GroupDuration.Rx().Text())
                .DisposedBy(DisposeBag);

            CloseButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            ConfirmButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            DescriptionTextView.TextObservable
                .Subscribe(ViewModel.Description.Accept)
                .DisposedBy(DisposeBag);

            DescriptionTextView.SizeChangedObservable
                .Subscribe(adjustHeight)
                .DisposedBy(DisposeBag);

            ViewModel.SyncErrorMessage
                .Subscribe(ErrorMessageLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.IsSyncErrorMessageVisible
                .Subscribe(ErrorView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ErrorView.Rx().Tap()
                .Subscribe(ViewModel.DismissSyncErrorMessage.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(info => projectTaskClientToAttributedString.Convert(
                    info.Project,
                    info.Task,
                    info.Client,
                    new Color(info.ProjectColor).ToNativeColor()))
                .Subscribe(ProjectTaskClientLabel.Rx().AttributedText())
                .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(info => info.HasProject)
                .Subscribe(ProjectTaskClientLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.ProjectClientTask
                .Select(info => !info.HasProject)
                .Subscribe(AddProjectAndTaskView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            SelectProject.Rx()
                .BindAction(ViewModel.SelectProject)
                .DisposedBy(DisposeBag);

            TagsTextView.Rx()
                .BindAction(ViewModel.SelectTags)
                .DisposedBy(DisposeBag);

            AddTagsView.Rx()
                .BindAction(ViewModel.SelectTags)
                .DisposedBy(DisposeBag);

            var containsTags = ViewModel.Tags
                .Select(tags => tags.Any());

            containsTags
                .Invert()
                .Subscribe(AddTagsView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            containsTags
                .Subscribe(TagsTextView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsBillable
                .Subscribe(BillableSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            BillableSwitch.Rx().Changed()
                .Subscribe(ViewModel.ToggleBillable.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsBillableAvailable
                .Subscribe(BillableView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsInaccessible
                .Subscribe(adjustUIForInaccessibleTimeEntry)
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(
                        startTime,
                        preferences.TimeOfDayFormat.Format,
                        IosDependencyContainer.Instance.AnalyticsService))
                .Subscribe(StartTimeLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.StartTime
                .WithLatestFrom(ViewModel.Preferences,
                    (startTime, preferences) => DateTimeToFormattedString.Convert(
                        startTime,
                        preferences.DateFormat.Short,
                        IosDependencyContainer.Instance.AnalyticsService))
                .Subscribe(StartDateLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            StartTimeView.Rx().Tap()
                .SelectValue(EditViewTapSource.StartTime)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            StartDateView.Rx().Tap()
                .Subscribe(ViewModel.SelectStartDate.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Subscribe(StopButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Select(CommonFunctions.Invert)
                .Subscribe(EndTimeLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.StopTime
                .Where(stopTime => stopTime.HasValue)
                .Select(stopTime => stopTime.Value)
                .WithLatestFrom(ViewModel.Preferences,
                    (stopTime, preferences) => DateTimeToFormattedString.Convert(
                        stopTime,
                        preferences.TimeOfDayFormat.Format,
                        IosDependencyContainer.Instance.AnalyticsService))
                .Subscribe(EndTimeLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            EndTimeView.Rx().Tap()
                .SelectLatestFrom(ViewModel.IsTimeEntryRunning)
                .Invert()
                .Where(CommonFunctions.Identity)
                .SelectValue(EditViewTapSource.StopTime)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            EndTimeView.Rx().Tap()
               .Merge(StopButton.Rx().Tap())
               .SelectLatestFrom(ViewModel.IsTimeEntryRunning)
               .Where(CommonFunctions.Identity)
               .SelectUnit()
               .Subscribe(ViewModel.StopTimeEntry.Inputs)
               .DisposedBy(DisposeBag);

            ViewModel.Duration
                .WithLatestFrom(ViewModel.Preferences,
                    (duration, preferences) => duration.ToFormattedString(preferences.DurationFormat))
                .Subscribe(DurationLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            DurationView.Rx().Tap()
                .SelectValue(EditViewTapSource.Duration)
                .Subscribe(ViewModel.EditTimes.Inputs)
                .DisposedBy(DisposeBag);

            DeleteButton.Rx()
                .BindAction(ViewModel.Delete)
                .DisposedBy(DisposeBag);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            ViewModel.Tags
                .Select(tagsListToAttributedString.Convert)
                .Subscribe(TagsTextView.Rx().AttributedTextObserver())
                .DisposedBy(DisposeBag);

            View.ClipsToBounds |= UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();

            View.ClipsToBounds |= UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
        }

        private void prepareViews()
        {
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();

            centerTextVertically(TagsTextView);
            TagsTextView.TextContainer.LineFragmentPadding = 0;

            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                && UIDevice.CurrentDevice.UserInterfaceIdiom != UIUserInterfaceIdiom.Pad)
            {
                var bottomSafeAreaInset = UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
                if (bottomSafeAreaInset >= ButtonsContainerBottomConstraint.Constant)
                    ButtonsContainerBottomConstraint.Constant = 0;
            }

            DescriptionTextView.TintColor = Colors.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.PlaceholderText = Resources.AddDescription;

            TimeEntryTimes.Hidden = ViewModel.IsEditingGroup;
            TimeEntryTimesSeparator.Hidden = ViewModel.IsEditingGroup;
            GroupDuration.Hidden = !ViewModel.IsEditingGroup;
            DurationView.Hidden = ViewModel.IsEditingGroup;
            StartDateView.Hidden = ViewModel.IsEditingGroup;
            DurationSeparator.Hidden = ViewModel.IsEditingGroup;
            StartDateSeparator.Hidden = ViewModel.IsEditingGroup;
        }

        private void localizeLabels()
        {
            TitleLabel.Text = ViewModel.IsEditingGroup
                ? string.Format(Resources.EditingTimeEntryGroup, ViewModel.GroupCount)
                : Resources.Edit;

            BillableLabel.Text = Resources.Billable;
            StartDateDescriptionLabel.Text = Resources.Startdate;
            DurationDescriptionLabel.Text = Resources.Duration;
            StartDescriptionLabel.Text = Resources.Start;
            EndDescriptionLabel.Text = Resources.End;
            ErrorMessageTitleLabel.Text = Resources.Oops;
            AddProjectTaskLabel.Text = Resources.AddProjectTask;
            CategorizeWithProjectsLabel.Text = Resources.CategorizeYourTimeWithProjects;
            AddTagsLabel.Text = Resources.AddTags;
            DeleteButton.SetTitle(Resources.Delete, UIControlState.Normal);
            ConfirmButton.SetTitle(Resources.ConfirmChanges, UIControlState.Normal);
        }

        private void adjustUIForInaccessibleTimeEntry(bool isInaccessible)
        {
            DescriptionTextView.UserInteractionEnabled = !isInaccessible;
            StartTimeView.UserInteractionEnabled = !isInaccessible;
            StartDateView.UserInteractionEnabled = !isInaccessible;
            EndTimeView.UserInteractionEnabled = !isInaccessible;
            DurationView.UserInteractionEnabled = !isInaccessible;
            StopButton.UserInteractionEnabled = !isInaccessible;

            BillableSwitch.Enabled = !isInaccessible;
            TagsContainerView.Hidden = isInaccessible;
            TagsSeparator.Hidden = isInaccessible;

            var textColor = isInaccessible
                ? Colors.Common.Disabled.ToNativeColor()
                : Colors.Common.TextColor.ToNativeColor();

            DescriptionTextView.TextColor = textColor;

            StartTimeLabel.TextColor = textColor;
            StartDateLabel.TextColor = textColor;
            EndTimeLabel.TextColor = textColor;
            DurationLabel.TextColor = textColor;
        }

        private void centerTextVertically(UITextView textView)
        {
            var topOffset = (textView.Bounds.Height - textView.ContentSize.Height) / 2;
            textView.ContentInset = new UIEdgeInsets(topOffset, 0, 0, 0);
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            projectOnboardingDisposable = new CategorizeTimeUsingProjectsOnboardingStep(storage, ViewModel.ProjectClientTask.Select(x => x.HasProject).AsObservable())
                .ManageDismissableTooltip(CategorizeWithProjectsBubbleView, storage);
        }

        private void onContentSizeChanged(NSObservedChange change)
        {
            adjustHeight();
        }

        private void adjustHeight()
        {
            double height;
            nfloat coveredByKeyboard = 0;

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                height = nonScrollableContentHeight + ScrollViewContent.Bounds.Height;
                if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                {
                    height += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Bottom;
                }

                if (keyboardHeight > 0)
                {
                    // this bit of code depends on the knowledge of the component tree:
                    // - description label is inside of a container view which is placed in a stack view
                    // - we want to know the vertical location of the container view in the whole view
                    // - we actually want to know the Y coordinate of the bottom of the container and make
                    //   sure we don't overaly it with the keyboard
                    var container = DescriptionTextView.Superview;
                    var absoluteLocation = View.ConvertPointFromView(container.Frame.Location, container.Superview.Superview);
                    var minimumVisibleContentHeight = View.Frame.Height - absoluteLocation.Y - container.Frame.Height;

                    coveredByKeyboard = keyboardHeight - minimumVisibleContentHeight;

                    var safeAreaOffset = UIDevice.CurrentDevice.CheckSystemVersion(11, 0)
                        ? Math.Max(UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Top, UIApplication.SharedApplication.StatusBarFrame.Height)
                        : 0;
                    var distanceFromTop = Math.Max(safeAreaOffset, View.Frame.Y - coveredByKeyboard);

                    height = UIScreen.MainScreen.Bounds.Height - distanceFromTop;
                }
            }
            else
            {
                var errorHeight = ErrorView.Hidden
                    ? 0
                    : ErrorView.SizeThatFits(UIView.UILayoutFittingCompressedSize).Height;
                var titleHeight = DescriptionView.SizeThatFits(UIView.UILayoutFittingCompressedSize).Height;
                var isBillableHeight = BillableView.Hidden ? 0 : 56;
                var timeFieldsHeight = ViewModel.IsEditingGroup ? 0 : 167;
                height = preferredIpadHeight + errorHeight + titleHeight + timeFieldsHeight + isBillableHeight;
            }

            var newSize = new CGSize(0, height);

            if (newSize != PreferredContentSize)
            {
                PreferredContentSize = newSize;
                PresentationController.ContainerViewWillLayoutSubviews();
            }

            ScrollView.ScrollEnabled = ScrollViewContent.Bounds.Height > ScrollView.Bounds.Height;

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                if (ScrollView.ScrollEnabled && keyboardHeight > 0)
                {
                    nfloat scrollViewContentBottomPadding = ViewModel.IsEditingGroup ? coveredByKeyboard : 0;
                    ScrollView.ContentSize = new CGSize(ScrollView.ContentSize.Width, ScrollViewContent.Bounds.Height + scrollViewContentBottomPadding);
                    ScrollView.SetContentOffset(new CGPoint(0, ScrollView.ContentSize.Height - ScrollView.Bounds.Height), false);
                }
                else
                {
                    ScrollView.ContentSize = new CGSize(ScrollView.ContentSize.Width, ScrollViewContent.Bounds.Height);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            contentSizeChangedDisposable?.Dispose();
            contentSizeChangedDisposable = null;

            projectOnboardingDisposable?.Dispose();
            projectOnboardingDisposable = null;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            keyboardHeight = (float)e.FrameEnd.Height;
            adjustHeight();
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            keyboardHeight = 0;
            adjustHeight();
        }
    }
}
