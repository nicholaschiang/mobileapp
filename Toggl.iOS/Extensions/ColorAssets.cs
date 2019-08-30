using System.Drawing;
using UIKit;

namespace Toggl.iOS.Extensions
{
    public static class ColorAssets
    {
        private static class CustomColors
        {
            public static readonly UIColor Background = UIColor.FromName("Background");
            public static readonly UIColor SecondaryBackground = UIColor.FromName("SecondaryBackground");
            public static readonly UIColor TertiaryBackground = UIColor.FromName("TertiaryBackground");

            public static readonly UIColor GroupedBackground = UIColor.FromName("GroupedBackground");
            public static readonly UIColor SecondaryGroupedBackground = UIColor.FromName("SecondaryGroupedBackground");
            public static readonly UIColor TertiaryGroupedBackground = UIColor.FromName("TertiaryGroupedBackground");

            public static readonly UIColor Label = UIColor.FromName("Label");
            public static readonly UIColor SecondaryLabel = UIColor.FromName("SecondaryLabel");
            public static readonly UIColor TertiaryLabel = UIColor.FromName("TertiaryLabel");
            public static readonly UIColor QuaternaryLabel = UIColor.FromName("QuaternaryLabel");

            public static readonly UIColor Separator = UIColor.FromName("Separator");
            public static readonly UIColor OpaqueSeparator = UIColor.FromName("OpaqueSeparator");

            public static readonly UIColor CustomGray = UIColor.FromName("CustomGray");
            public static readonly UIColor CustomGray2 = UIColor.FromName("CustomGray2");
            public static readonly UIColor CustomGray3 = UIColor.FromName("CustomGray3");
            public static readonly UIColor CustomGray4 = UIColor.FromName("CustomGray4");
            public static readonly UIColor CustomGray5 = UIColor.FromName("CustomGray5");
            public static readonly UIColor CustomGray6 = UIColor.FromName("CustomGray6");

            public static readonly UIColor Green = UIColor.FromName("Green");
        }

        public static readonly UIColor Background = CustomColors.Background;
        public static readonly UIColor AlternateBackground = CustomColors.SecondaryBackground;
        public static readonly UIColor Text = CustomColors.Label;
        public static readonly UIColor SecondaryText = CustomColors.SecondaryLabel;
        public static readonly UIColor Placeholder = CustomColors.CustomGray4;

        public static class Navigation
        {
            public static readonly UIColor Title = CustomColors.CustomGray2;
            public static readonly UIColor BarButtons = CustomColors.CustomGray2;
        }

        public static class Table
        {
            public static readonly UIColor HeaderBackground = CustomColors.GroupedBackground;
            public static readonly UIColor CellBackground = CustomColors.SecondaryGroupedBackground;
            public static readonly UIColor HeaderText = CustomColors.SecondaryLabel;
            public static readonly UIColor Separator = CustomColors.Separator;
        }

        public static class Timeline
        {
            public static readonly UIColor GroupedCellBackground = CustomColors.CustomGray6;
            public static readonly UIColor Arrow = CustomColors.CustomGray4;
            public static readonly UIColor GroupCount = CustomColors.CustomGray2;
            public static readonly UIColor GroupCountBorder = CustomColors.Separator;
            public static readonly UIColor GroupCountBackground = CustomColors.CustomGray5;
            public static readonly UIColor GroupCountExpanded = CustomColors.Green;
            public static readonly UIColor ClientLabel = CustomColors.CustomGray3;
        }

        public static class RefreshControl
        {
            public static readonly UIColor Background = CustomColors.CustomGray3;
            public static readonly UIColor SyncCompletedBackground = CustomColors.Green;
            public static readonly UIColor Text = UIColor.White;
        }

        public static class Suggestions
        {
            public static readonly UIColor Background = CustomColors.SecondaryBackground;
        }
    }
}
