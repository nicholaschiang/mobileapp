using Toggl.Core.UI.Helper;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS
{
    public partial class AppDelegate
    {
        private void setupTabBar()
        {
            UITabBar.Appearance.SelectedImageTintColor = Colors.TabBar.SelectedImageTintColor.ToNativeColor();
            UITabBarItem.Appearance.TitlePositionAdjustment = new UIOffset(0, 200);
        }

        private void setupNavigationBar()
        {
            //Back button title
            var attributes = new UITextAttributes
            {
                Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium),
                TextColor = ColorAssets.CustomGray2
            };
            UIBarButtonItem.Appearance.SetTitleTextAttributes(attributes, UIControlState.Normal);
            UIBarButtonItem.Appearance.SetTitleTextAttributes(attributes, UIControlState.Highlighted);
            UIBarButtonItem.Appearance.SetBackButtonTitlePositionAdjustment(new UIOffset(6, 0), UIBarMetrics.Default);
        }
    }
}
