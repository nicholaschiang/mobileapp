using UIKit;

namespace Toggl.iOS.Extensions
{
    public static class ColorAssets
    {
        public static readonly UIColor MainBackground = UIColor.FromName("MainBackground");
        public static readonly UIColor AlternateBackground = UIColor.FromName("AlternateBackground");
        public static readonly UIColor Separator = UIColor.FromName("Separator");
        public static readonly UIColor Text = UIColor.FromName("Text");
        public static readonly UIColor PlaceholderText = UIColor.FromName("PlaceholderText");

        public static class Timeline
        {
            public static readonly UIColor ExpandedGroup = UIColor.FromName("Timeline_ExpandedGroup");
            public static readonly UIColor CollapsedGroup = UIColor.FromName("Timeline_CollapsedGroup");
            public static readonly UIColor ClientName = UIColor.FromName("Timeline_ClientName");
            public static readonly UIColor Arrow = UIColor.FromName("Timeline_Arrow");
        }
    }
}
