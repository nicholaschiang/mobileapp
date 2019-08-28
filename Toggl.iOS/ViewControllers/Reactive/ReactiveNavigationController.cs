using System.Linq;
using Toggl.Core.UI.Views;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public class ReactiveNavigationController : UINavigationController
    {
        public ReactiveNavigationController(UIViewController rootViewController) : base(rootViewController)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            setBarAppereance();
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            setBarAppereance();
        }

        public override UIViewController PopViewController(bool animated)
        {
            var viewControllerToPop = ViewControllers.Last();
            if (viewControllerToPop is IReactiveViewController reactiveViewController)
            {
                reactiveViewController.DismissFromNavigationController();
            }

            return base.PopViewController(animated);
        }

        private void setBarAppereance()
        {
            //Back button icon
            var image = UIImage.FromBundle("icBackNoPadding");
            NavigationBar.BackIndicatorImage = image;
            NavigationBar.BackIndicatorTransitionMaskImage = image;

            //Title and background
            var barBackgroundColor = ColorAssets.AlternateBackground;
            NavigationBar.ShadowImage = new UIImage();
            NavigationBar.BarTintColor = barBackgroundColor;
            NavigationBar.BackgroundColor = barBackgroundColor;
            NavigationBar.TintColor = ColorAssets.CustomGray2;
            NavigationBar.SetBackgroundImage(ImageExtension.ImageWithColor(barBackgroundColor), UIBarMetrics.Default);
            NavigationBar.TitleTextAttributes = new UIStringAttributes
            {
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium),
                ForegroundColor = ColorAssets.Text
            };
        }
    }
}
