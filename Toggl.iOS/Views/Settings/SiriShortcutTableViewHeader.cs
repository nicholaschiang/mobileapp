using Foundation;
using System;
using Toggl.iOS.Cells;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.Views.Settings
{
    public partial class SiriShortcutTableViewHeader : BaseTableHeaderFooterView<string>
    {
        public static readonly string Identifier = nameof(SiriShortcutTableViewHeader);
        public static readonly UINib Nib;

        public bool TopSeparatorHidden
        {
            get => TopSeparator.Hidden;
            set => TopSeparator.Hidden = value;
        }

        static SiriShortcutTableViewHeader()
        {
            Nib = UINib.FromName("SiriShortcutTableViewHeader", NSBundle.MainBundle);
        }

        protected SiriShortcutTableViewHeader(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ContentView.BackgroundColor = ColorAssets.AlternateBackground;
            TitleLabel.TextColor = ColorAssets.CustomGray4;
            TopSeparator.BackgroundColor = ColorAssets.Separator;
            BottomSeparator.BackgroundColor = ColorAssets.Separator;
        }

        protected override void UpdateView()
        {
            TitleLabel.Text = Item;
        }
    }
}

