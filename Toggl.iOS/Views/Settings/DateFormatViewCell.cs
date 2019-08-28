using Foundation;
using System;
using Toggl.Core.UI.ViewModels.Selectable;
using Toggl.iOS.Cells;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.Views.Settings
{
    public sealed partial class DateFormatViewCell : BaseTableViewCell<SelectableDateFormatViewModel>
    {
        public static readonly string Identifier = nameof(DateFormatViewCell);

        public static readonly NSString Key = new NSString(nameof(DateFormatViewCell));
        public static readonly UINib Nib;

        static DateFormatViewCell()
        {
            Nib = UINib.FromName(nameof(DateFormatViewCell), NSBundle.MainBundle);
        }

        public DateFormatViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ContentView.BackgroundColor = ColorAssets.Background;
            DateFormatLabel.TextColor = ColorAssets.Text;
            Separator.BackgroundColor = ColorAssets.Separator;

            DateFormatLabel.Text = string.Empty;
            SelectedImageView.Hidden = true;
        }

        protected override void UpdateView()
        {
            DateFormatLabel.Text = Item.DateFormat.Localized;
            SelectedImageView.Hidden = !Item.Selected;
        }
    }
}
