using Foundation;
using System;
using Toggl.Core.UI.Transformations;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Cells;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.Views.Settings
{
    public sealed partial class DurationFormatViewCell : BaseTableViewCell<SelectableDurationFormatViewModel>
    {
        public static readonly string Identifier = nameof(DurationFormatViewCell);

        public static readonly NSString Key = new NSString(nameof(DurationFormatViewCell));
        public static readonly UINib Nib;

        static DurationFormatViewCell()
        {
            Nib = UINib.FromName(nameof(DurationFormatViewCell), NSBundle.MainBundle);
        }

        public DurationFormatViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ContentView.BackgroundColor = ColorAssets.Background;
            DurationFormatLabel.TextColor = ColorAssets.Text;
            Separator.BackgroundColor = ColorAssets.Table.Separator;

            DurationFormatLabel.Text = string.Empty;
            SelectedImageView.Hidden = true;
        }

        protected override void UpdateView()
        {
            DurationFormatLabel.Text = DurationFormatToString.Convert(Item.DurationFormat);
            SelectedImageView.Hidden = !Item.Selected;
        }
    }
}
