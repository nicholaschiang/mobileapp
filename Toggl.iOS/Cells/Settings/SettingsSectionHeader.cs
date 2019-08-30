using System;
using Foundation;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.Cells.Settings
{
    public partial class SettingsSectionHeader : BaseTableHeaderFooterView<string>
    {
        public static readonly string Identifier = new NSString("SettingsSectionHeader");
        public static readonly UINib Nib;

        static SettingsSectionHeader()
        {
            Nib = UINib.FromName("SettingsSectionHeader", NSBundle.MainBundle);
        }

        protected SettingsSectionHeader(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            ContentView.BackgroundColor = ColorAssets.Table.HeaderBackground;
            TitleLabel.TextColor = ColorAssets.Table.HeaderText;
            BottomSeparator.BackgroundColor = ColorAssets.Table.Separator;
        }

        protected override void UpdateView()
        {
            TitleLabel.Text = Item;
        }
    }
}

