using System;
using CoreGraphics;
using Foundation;
using Toggl.iOS.Extensions;
using UIKit;
using static Toggl.Core.UI.Helper.Animation;

namespace Toggl.iOS.Presentation.Transition
{
    public sealed class ModalCardTransition : NSObject, IUIViewControllerAnimatedTransitioning
    {
        private readonly bool presenting;

        public ModalCardTransition(bool presenting)
        {
            this.presenting = presenting;
        }

        public double TransitionDuration(IUIViewControllerContextTransitioning transitionContext)
            => presenting ? Timings.EnterTiming : Timings.LeaveTiming;

        public void AnimateTransition(IUIViewControllerContextTransitioning transitionContext)
        {
            var toController = transitionContext.GetViewControllerForKey(UITransitionContext.ToViewControllerKey);
            var fromController = transitionContext.GetViewControllerForKey(UITransitionContext.FromViewControllerKey);
            var animationDuration = TransitionDuration(transitionContext);

            if (presenting)
            {
                transitionContext.ContainerView.AddSubview(toController.View);

                var toControllerInitialFrame = transitionContext.GetInitialFrameForViewController(fromController);
                var toControllerFinalFrame = transitionContext.GetFinalFrameForViewController(toController);

                toControllerInitialFrame.Offset(0, toController.View.Frame.Height);
                toController.View.Frame = toControllerInitialFrame;

                AnimationExtensions.Animate(animationDuration, Curves.CardInCurve, () =>
                {
                    applyTransformation(fromController);
                    toController.View.Frame = toControllerFinalFrame;
                },
                () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
            }
            else
            {
                var fromControllerInitialFrame = transitionContext.GetInitialFrameForViewController(fromController);
                var fromControllerFinalFrame = transitionContext.GetFinalFrameForViewController(fromController);

                fromControllerFinalFrame.Offset(0, fromController.View.Frame.Height);
                fromController.View.Frame = fromControllerInitialFrame;

                AnimationExtensions.Animate(animationDuration, Curves.CardOutCurve, () =>
                {
                    fromController.View.Frame = fromControllerFinalFrame;
                    removeTransformation(toController);
                },
                () => transitionContext.CompleteTransition(!transitionContext.TransitionWasCancelled));
            }
        }

        private void applyTransformation(UIViewController viewController)
        {
            if (UIDevice.CurrentDevice.UserInterfaceIdiom != UIUserInterfaceIdiom.Phone)
                return;

            var transformation = CGAffineTransform.MakeIdentity();
            transformation.Scale((nfloat)0.95, (nfloat)0.95);
            viewController.View.Transform = transformation;
        }

        private void removeTransformation(UIViewController viewController)
        {
            var transformation = CGAffineTransform.MakeIdentity();
            viewController.View.Transform = transformation;
        }
    }
}
