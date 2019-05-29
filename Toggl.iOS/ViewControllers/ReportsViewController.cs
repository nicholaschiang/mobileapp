using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.ViewModels.Reports;
using Toggl.iOS.Presentation;
using Toggl.iOS.Views.Reports;
using Toggl.iOS.ViewSources;
using Toggl.Shared.Extensions;
using UIKit;
using static Toggl.iOS.Extensions.AnimationExtensions;
using Toggl.Core.UI.Views;

namespace Toggl.iOS.ViewControllers
{
    public sealed partial class ReportsViewController : ReactiveViewController<ReportsViewModel>, IScrollableToTop
    {
        private const string boundsKey = "bounds";

        private const double maximumWorkspaceNameLabelWidthCompact = 144;
        private const double maximumWorkspaceNameLabelWidthRegular = 288;

        private const double maxWidth = 834;


        private UIButton titleButton;
        private bool calendarIsVisible;
        private bool alreadyLoadedCalendar;
        private ReportsTableViewSource source;
        private IDisposable calendarSizeDisposable;
        private ReportsCalendarViewController calendarViewController;
        private nfloat calendarHeight => CalendarContainer.Bounds.Height;
        private ReportsOverviewCardView overview = ReportsOverviewCardView.CreateFromNib();
        private ReportsBarChartCardView barChart = ReportsBarChartCardView.CreateFromNib();

        public ReportsViewController(ReportsViewModel viewModel) : base(viewModel, nameof(ReportsViewController))
        {
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            ReportsTableView.ReloadData();
        }

        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate(fromInterfaceOrientation);

            HideCalendar();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            calendarViewController = ViewControllerLocator.GetViewController(ViewModel.CalendarViewModel) as ReportsCalendarViewController;
            prepareViews();

            OverviewContainerView.AddSubview(overview);
            overview.Frame = OverviewContainerView.Bounds;
            overview.Item = ViewModel;
            BarChartsContainerView.AddSubview(barChart);
            barChart.Frame = BarChartsContainerView.Bounds;
            barChart.Item = ViewModel;

            calendarSizeDisposable = CalendarContainer.AddObserver(boundsKey, NSKeyValueObservingOptions.New, onCalendarSizeChanged);

            source = new ReportsTableViewSource(ReportsTableView, ViewModel);

            ViewModel.SegmentsObservable
                .Subscribe(ReportsTableView.Rx().ReloadItems(source))
                .DisposedBy(DisposeBag);

            source.ScrolledWithHeaderOffset
                .Subscribe(onReportsTableScrolled)
                .DisposedBy(DisposeBag);

            ReportsTableView.Source = source;

            bool areThereEnoughWorkspaces(ICollection<SelectOption<IThreadSafeWorkspace>> workspaces) => workspaces.Count > 1;

            bool isWorkspaceNameTooLong(string workspaceName)
            {
                var attributes = new UIStringAttributes { Font = WorkspaceLabel.Font };
                var size = new NSString(workspaceName).GetSizeUsingAttributes(attributes);
                var maxWidth = TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular
                    ? maximumWorkspaceNameLabelWidthRegular
                    : maximumWorkspaceNameLabelWidthCompact;
                return size.Width >= maxWidth;
            };

            //Text
            ViewModel.WorkspaceNameObservable
                .Subscribe(WorkspaceLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentDateRangeStringObservable
                .Subscribe(titleButton.Rx().Title())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.WorkspacesObservable
                .Select(areThereEnoughWorkspaces)
                .Subscribe(WorkspaceButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceNameObservable
                .Select(isWorkspaceNameTooLong)
                .Subscribe(WorkspaceFadeView.Rx().FadeRight())
                .DisposedBy(DisposeBag);

            //Commands
            titleButton.Rx().Tap()
                .Subscribe(toggleCalendar)
                .DisposedBy(DisposeBag);

            ReportsTableView.Rx().Tap()
                .Subscribe(HideCalendar)
                .DisposedBy(DisposeBag);

            WorkspaceButton.Rx()
                .BindAction(ViewModel.SelectWorkspace)
                .DisposedBy(DisposeBag);

            void toggleCalendar()
            {
                if (calendarIsVisible)
                {
                    HideCalendar();
                    return;
                }

                if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact)
                {
                    ShowCalendar();
                    return;
                }

                ShowPopoverCalendar();
            }
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            if (alreadyLoadedCalendar)
                return;

            // Calendar
            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            {
                AddChildViewController(calendarViewController);
                CalendarContainer.AddSubview(calendarViewController.View);

                calendarViewController.View.TopAnchor.ConstraintEqualTo(CalendarContainer.TopAnchor).Active = true;
                calendarViewController.View.BottomAnchor.ConstraintEqualTo(CalendarContainer.BottomAnchor).Active = true;
                calendarViewController.View.LeftAnchor.ConstraintEqualTo(CalendarContainer.LeftAnchor).Active = true;
                calendarViewController.View.RightAnchor.ConstraintEqualTo(CalendarContainer.RightAnchor).Active = true;
                calendarViewController.View.TranslatesAutoresizingMaskIntoConstraints = false;
                calendarViewController.DidMoveToParentViewController(this);
            }

            alreadyLoadedCalendar = true;
        }

        public void ScrollToTop()
        {
            var point = new CGPoint(0, -ReportsTableView.ContentInset.Top);
            ReportsTableView.SetContentOffset(point, true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            calendarSizeDisposable?.Dispose();
            calendarSizeDisposable = null;
        }

        private void onReportsTableScrolled(CGPoint offset)
        {
            if (calendarIsVisible)
            {
                var topConstant = (TopCalendarConstraint.Constant + offset.Y).Clamp(0, calendarHeight);
                TopCalendarConstraint.Constant = topConstant;

                if (topConstant == 0) return;

                // we need to adjust the offset of the scroll view so that it doesn't fold
                // under the calendar while scrolling up
                var adjustedOffset = new CGPoint(offset.X, ReportsTableView.ContentOffset.Y - offset.Y);
                ReportsTableView.SetContentOffset(adjustedOffset, false);
                View.LayoutIfNeeded();

                if (topConstant == calendarHeight)
                {
                    HideCalendar();
                }
            }
        }

        internal void ShowCalendar()
        {
            TopCalendarConstraint.Constant = 0;
            Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => View.LayoutIfNeeded(),
                () => {
                    calendarIsVisible = true;
                    ViewModel.CalendarViewModel.Reload();
                });
        }

        internal void HideCalendar()
        {
            calendarViewController.DismissViewController(false, null);

            TopCalendarConstraint.Constant = calendarHeight;
            Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.SharpCurve,
                () => View.LayoutIfNeeded(),
                () => calendarIsVisible = false);
        }

        internal void ShowPopoverCalendar()
        {
            HideCalendar();

            calendarViewController.ModalPresentationStyle = UIModalPresentationStyle.Popover;
            UIPopoverPresentationController presentationPopover = calendarViewController.PopoverPresentationController;
            if (presentationPopover != null)
            {
                presentationPopover.SourceView = titleButton;
                presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Up;
                presentationPopover.SourceRect = titleButton.Frame;
            }

            PresentViewController(calendarViewController, true, null);
            ViewModel.CalendarViewModel.Reload();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            source.UpdateContentInset();
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();
            ContentWidthConstraint.Constant = (nfloat) Math.Min(View.Bounds.Width, maxWidth);
        }

        private void prepareViews()
        {
            // Title view
            NavigationItem.TitleView = titleButton = new UIButton(new CGRect(0, 0, 200, 40));
            titleButton.Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium);
            titleButton.SetTitleColor(UIColor.Black, UIControlState.Normal);

            // Calendar configuration
            TopCalendarConstraint.Constant = calendarHeight;

            // Workspace button settings
            WorkspaceFadeView.FadeWidth = 32;
            WorkspaceButton.Layer.ShadowColor = UIColor.Black.CGColor;
            WorkspaceButton.Layer.ShadowRadius = 10;
            WorkspaceButton.Layer.ShadowOffset = new CGSize(0, 2);
            WorkspaceButton.Layer.ShadowOpacity = 0.10f;
        }

        private void onCalendarSizeChanged(NSObservedChange change)
        {
            TopCalendarConstraint.Constant = calendarIsVisible ? 0 : calendarHeight;
        }
    }
}

