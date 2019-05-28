using System;
using Foundation;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels.ReportsCalendar;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.Views
{
    public sealed partial class ReportsCalendarViewCell : ReactiveCollectionViewCell<ReportsCalendarDayViewModel>
    {
        private const int cornerRadius = 16;

        public static readonly NSString Key = new NSString(nameof(ReportsCalendarViewCell));
        public static readonly UINib Nib;

        private ReportsDateRange dateRange;

        static ReportsCalendarViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarViewCell), NSBundle.MainBundle);
        }

        public ReportsCalendarViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            prepareViews();
        }

        private void prepareViews()
        {
            //Background view
            BackgroundView.CornerRadius = cornerRadius;

            //Today background indicator
            TodayBackgroundView.CornerRadius = cornerRadius;
            TodayBackgroundView.RoundLeft = true;
            TodayBackgroundView.RoundRight = true;
            TodayBackgroundView.BackgroundColor = Colors.ReportsCalendar.Today.ToNativeColor();
        }

        private readonly UIColor otherMonthColor = Colors.ReportsCalendar.CellTextColorOutOfCurrentMonth.ToNativeColor();
        private readonly UIColor thisMonthColor = Colors.ReportsCalendar.CellTextColorInCurrentMonth.ToNativeColor();
        private readonly UIColor selectedColor = Colors.ReportsCalendar.CellTextColorSelected.ToNativeColor();

        protected override void UpdateView()
        {
            Text.Text = Item.Day.ToString();

            if (Item.IsSelected(dateRange))
            {
                Text.TextColor = selectedColor;
            }
            else
            {
                Text.TextColor = Item.IsInCurrentMonth ? thisMonthColor : otherMonthColor;
            }

            BackgroundView.BackgroundColor = Item.IsSelected(dateRange)
                ? Colors.ReportsCalendar.SelectedDayBackgoundColor.ToNativeColor()
                : Colors.Common.Transparent.ToNativeColor();

            BackgroundView.RoundLeft = Item.IsStartOfSelectedPeriod(dateRange);
            BackgroundView.RoundRight = Item.IsEndOfSelectedPeriod(dateRange);
            TodayBackgroundView.Hidden = !Item.IsToday;
        }

        public void UpdateDateRange(ReportsDateRange dateRange)
        {
            this.dateRange = dateRange;
        }
    }
}
