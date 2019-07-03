using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toggl.Core.UI.Helper;
using Toggl.iOS.Extensions;
using Toggl.iOS.ViewControllers;
using UIKit;
using static System.Math;

namespace Toggl.iOS.Presentation.Transition
{
    public sealed class ModalCardPresentationController : UIPresentationController
    {
        private readonly UIImpactFeedbackGenerator feedbackGenerator;

        private readonly nfloat backgroundAlpha = 0.8f;
        private const double returnAnimationDuration = 0.1;
        private const double impactThreshold = 0.4;
        private const double iPadMinHeightWithoutKeyboard = 360;
        private const double iPadMaxWidth = 540;
        private const double iPadTopMarginNarrow = 40;
        private const double iPadTopMarginLarge = 76;
        private const double iPadStackModalViewSpacing = 40;

        private double topiPadMargin
            => PresentingViewController.TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact
               || UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeLeft
               || UIDevice.CurrentDevice.Orientation == UIDeviceOrientation.LandscapeRight
                ? iPadTopMarginNarrow
                : iPadTopMarginLarge;

        private double iPadMaxHeight => UIScreen.MainScreen.Bounds.Height - 2 * topiPadMargin;

        private CGPoint originalCenter;
        private double keyboardHeight = 0.0;

        private bool isKeyboardVisible => keyboardHeight != 0.0;

        private bool finishedPresenting = false;

        private List<NSObject> observers = new List<NSObject>();

        private nfloat maximumHeight
        {
            get
            {
                var distanceFromTop = UIApplication.SharedApplication.StatusBarFrame.Height;
                if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                {
                    distanceFromTop += UIApplication.SharedApplication.KeyWindow.SafeAreaInsets.Top;
                }
                return UIScreen.MainScreen.Bounds.Height - distanceFromTop;
            }
        }

        private readonly UIView dimmingView = new UIView
        {
            BackgroundColor = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone
                ? UIColor.Black.ColorWithAlpha(0.67f)
                : Colors.ModalDialog.BackgroundOverlay.ToNativeColor(),
            Alpha = 0
        };

        public UIView AdditionalContentView { get; }
            = new UIView();

        public ModalCardPresentationController(UIViewController presentedViewController, UIViewController presentingViewController)
            : base(presentedViewController, presentingViewController)
        {
            var recognizer = new UITapGestureRecognizer(() => dismiss());
            AdditionalContentView.AddGestureRecognizer(recognizer);

            var dismissBySwipingDown = new UIPanGestureRecognizer(handleSwipe);
            presentedViewController.View.AddGestureRecognizer(dismissBySwipingDown);

            feedbackGenerator = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Light);

            observers.AddRange(new[]
            {
                NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.WillChangeStatusBarFrameNotification, onStatusBarFrameChanged),
                NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardNotification),
                NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardNotification)
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            observers.ForEach(NSNotificationCenter.DefaultCenter.RemoveObserver);
            observers.Clear();
        }

        public override void PresentationTransitionWillBegin()
        {
            dimmingView.Frame = ContainerView.Bounds;
            AdditionalContentView.Frame = ContainerView.Bounds;
            AdditionalContentView.Layer.ZPosition += 1;

            ContainerView.AddSubview(dimmingView);
            ContainerView.AddSubview(AdditionalContentView);

            var coordinator = PresentedViewController.GetTransitionCoordinator();
            if (coordinator == null)
            {
                dimmingView.Alpha = backgroundAlpha;
                return;
            }

            coordinator.AnimateAlongsideTransition(_ => dimmingView.Alpha = backgroundAlpha, null);
        }

        public override void DismissalTransitionWillBegin()
        {
            var coordinator = PresentedViewController.GetTransitionCoordinator();
            if (coordinator == null)
            {
                dimmingView.Alpha = 0.0f;
                return;
            }

            coordinator.AnimateAlongsideTransition(_ => dimmingView.Alpha = 0.0f, null);
        }

        public override void DismissalTransitionDidEnd(bool completed)
        {
            base.DismissalTransitionDidEnd(completed);

            if (completed)
            {
                NSNotificationCenter.DefaultCenter.RemoveObserver(UIApplication.WillChangeStatusBarFrameNotification);
            }
        }

        public override void PresentationTransitionDidEnd(bool completed)
        {
            base.PresentationTransitionDidEnd(completed);
            finishedPresenting = completed;
        }

        public override void ContainerViewWillLayoutSubviews()
        {
            dimmingView.Frame = ContainerView.Bounds;
            PresentedView.Frame = FrameOfPresentedViewInContainerView;

            PresentedViewController.View.Layer.CornerRadius = 8.0f;
            PresentedViewController.View.Layer.MasksToBounds = false;
        }

        public override CGSize GetSizeForChildContentContainer(IUIContentContainer contentContainer, CGSize parentContainerSize)
        {
            var regularOrCompactHorizontalSizeClass = PresentingViewController.TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular
                                                      || PresentingViewController.TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact;
            var regularVerticalSizeClass = PresentingViewController.TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular;

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad
                && regularOrCompactHorizontalSizeClass
                && regularVerticalSizeClass)
            {
                var preferredContentHeight = PresentedViewController.PreferredContentSize.Height != 0
                    ? PresentedViewController.PreferredContentSize.Height
                    : iPadMaxHeight;

                var height = preferredContentHeight;
                var width = Min(iPadMaxWidth, parentContainerSize.Width);
                var stackingDepth = iPadStackModalViewSpacing * levelsOfModalViews();

                if (isKeyboardVisible)
                {
                    height = UIScreen.MainScreen.Bounds.Height - topiPadMargin - keyboardHeight - stackingDepth;
                }

                height = Min(height, iPadMaxHeight - stackingDepth);
                return new CGSize(width, height);
            }

            var preferredHeight = Min(maximumHeight, PresentedViewController.PreferredContentSize.Height);
            return new CGSize(parentContainerSize.Width, preferredHeight == 0 ? maximumHeight : preferredHeight);
        }

        public override CGRect FrameOfPresentedViewInContainerView
        {
            get
            {
                if (ContainerView == null)
                    return CGRect.Empty;

                var containerSize = ContainerView.Bounds.Size;
                var frame = CGRect.Empty;
                frame.Size = GetSizeForChildContentContainer(PresentedViewController, containerSize);
                frame.X = 0;
                frame.Y = containerSize.Height - frame.Size.Height;

                if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
                {
                    frame.X = (containerSize.Width - frame.Size.Width) / 2;

                    frame.Y = (containerSize.Height - frame.Size.Height) / 2 + (nfloat)iPadStackModalViewSpacing * levelsOfModalViews();

                    if (isKeyboardVisible || PresentingViewController.PresentingViewController != null)
                    {
                        frame.Y = (nfloat)topiPadMargin + (nfloat)iPadStackModalViewSpacing * levelsOfModalViews();
                    }
                }

                return frame;
            }
        }

        private int levelsOfModalViews()
        {
            var levels = 0;
            var topVC = PresentingViewController;
            while (topVC.PresentingViewController != null)
            {
                topVC = topVC.PresentingViewController;
                levels += 1;
            }

            return levels;
        }

        private async void handleSwipe(UIPanGestureRecognizer recognizer)
        {
            var translation = recognizer.TranslationInView(recognizer.View);
            var height = FrameOfPresentedViewInContainerView.Size.Height - keyboardHeight;
            var percent = (nfloat)Max(0, translation.Y / height);

            switch (recognizer.State)
            {
                case UIGestureRecognizerState.Began:
                    originalCenter = recognizer.View.Center;
                    feedbackGenerator.Prepare();
                    break;
                case UIGestureRecognizerState.Changed:
                    var center = new CGPoint(originalCenter.X, Max(originalCenter.Y, originalCenter.Y + translation.Y));
                    recognizer.View.Center = center;
                    dimmingView.Alpha = backgroundAlpha * (1 - percent);
                    break;
                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                    if (percent > impactThreshold)
                    {
                        dismiss();
                        feedbackGenerator.ImpactOccurred();
                    }
                    else
                    {
                        resetPosition(recognizer);
                    }
                    break;
            }
        }

        private void dismiss()
        {
            if (PresentedViewController is IReactiveViewController reactiveViewController)
            {
                reactiveViewController.DismissFromNavigationController();
                return;
            }

            PresentedViewController.DismissViewController(true, null);
        }

        private void resetPosition(UIGestureRecognizer recognizer)
        {
            UIView.Animate(returnAnimationDuration, () =>
            {
                dimmingView.Alpha = backgroundAlpha;
                recognizer.View.Center = originalCenter;
            });
        }

        private void onStatusBarFrameChanged(NSNotification notification)
        {
            ContainerView?.SetNeedsLayout();
        }

        private void OnKeyboardNotification(NSNotification notification)
        {
            var visible = notification.Name == UIKeyboard.WillShowNotification;
            keyboardHeight = visible
                ? UIKeyboard.FrameEndFromNotification(notification).Height
                : 0.0;

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad
                && finishedPresenting
                && ContainerView != null
                && PresentedViewController.PresentedViewController == null)
            {
                var animationDuration = UIKeyboard.AnimationDurationFromNotification(notification);
                AnimationExtensions.Animate(animationDuration, Animation.Curves.StandardEase, ContainerViewWillLayoutSubviews);
            }
            else if (PresentingViewController == null)
            {
                ContainerView?.SetNeedsLayout();
            }
        }
    }
}
