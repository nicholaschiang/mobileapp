using System;
using CoreGraphics;
using Foundation;
using Toggl.Core.UI.Helper;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.Presentation.Transition
{
    public sealed class ModalDialogTransition : NSObject, IUIViewControllerAnimatedTransitioning
    {
        private readonly bool presenting;

        public ModalDialogTransition(bool presenting)
        {
            this.presenting = presenting;
        }

        public double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            => presenting ? Animation.Timings.EnterTiming : Animation.Timings.LeaveTiming;

        public void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
        {
            var toController = transitionContext.GetViewControllerForKey(UITransitionContext.ToViewControllerKey);
            var fromController = transitionContext.GetViewControllerForKey(UITransitionContext.FromViewControllerKey);
            var animationDuration = TransitionDuration(transitionContext);

            if (presenting)
            {
                transitionContext.ContainerView.AddSubview(toController.View);

                var toControllerFinalFrame = transitionContext.GetFinalFrameForViewController(toController);

                var frame = new CGRect(toControllerFinalFrame.Location, toControllerFinalFrame.Size);
                frame.Offset(0.0f, transitionContext.ContainerView.Frame.Height - 20);
                toController.View.Frame = frame;
                toController.View.Alpha = 0.5f;

                AnimationExtensions.Animate(animationDuration, Animation.Curves.CardInCurve, () =>
                {
                    fromController.View.LayoutIfNeeded();
                    toController.View.Alpha = 1.0f;
                    toController.View.Frame = toControllerFinalFrame;
                },
                () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
            }
            else
            {

                var fromControllerInitialFrame = transitionContext.GetInitialFrameForViewController(fromController);
                var fromControllerFinalFrame = transitionContext.GetFinalFrameForViewController(fromController);

                fromControllerInitialFrame.Offset(0.0f, transitionContext.ContainerView.Frame.Height);
                var finalFrame = fromControllerInitialFrame;

                if (transitionContext.IsInteractive)
                {
                    AnimationExtensions.Animate(animationDuration, Animation.Curves.CardOutCurve, () =>
                    {
                        fromController.View.Frame = finalFrame;
                    },
                    () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
                }
                else
                {
                    AnimationExtensions.Animate(animationDuration, Animation.Curves.CardOutCurve, () =>
                    {
                        fromController.View.Frame = finalFrame;
                        fromController.View.Alpha = 0.5f;
                    },
                    () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
                }
            }
        }
    }
}
