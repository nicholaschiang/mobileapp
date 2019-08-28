using UIKit;

namespace Toggl.iOS.Extensions
{
    public static class ColorAssets
    {
        public static readonly UIColor Foreground = UIColor.FromName("Foreground");
        public static readonly UIColor Background = UIColor.FromName("Background");

        public static readonly UIColor CustomGray = UIColor.FromName("CustomGray");
        public static readonly UIColor CustomGray2 = UIColor.FromName("CustomGray2");
        public static readonly UIColor CustomGray3 = UIColor.FromName("CustomGray3");
        public static readonly UIColor CustomGray4 = UIColor.FromName("CustomGray4");
        public static readonly UIColor CustomGray5 = UIColor.FromName("CustomGray5");
        public static readonly UIColor CustomGray6 = UIColor.FromName("CustomGray6");

        public static readonly UIColor MainBackground = Background;
        public static readonly UIColor AlternateBackground = CustomGray6;
        public static readonly UIColor Text = Foreground;
        public static readonly UIColor Separator = CustomGray5;
        public static readonly UIColor Placeholder = CustomGray4;

        public static class Timeline
        {
            public static readonly UIColor ExpandedGroup = UIColor.FromName("Timeline_ExpandedGroup");
            public static readonly UIColor Arrow = CustomGray4;
        }
    }
}
