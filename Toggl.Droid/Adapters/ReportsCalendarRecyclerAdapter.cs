using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels.ReportsCalendar;
using Toggl.Droid.Extensions;
using Toggl.Droid.ViewHolders;

namespace Toggl.Droid.Adapters
{
    public sealed class ReportsCalendarRecyclerAdapter : BaseRecyclerAdapter<ReportsCalendarDayViewModel>
    {
        private static readonly int itemWidth;
        public ReportsDateRange DateRangeParameter { get; private set; }

        static ReportsCalendarRecyclerAdapter()
        {
            var context = Application.Context;
            var service = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
            var display = service.DefaultDisplay;
            var size = new Point();
            display.GetSize(size);

            itemWidth = size.X / 7;
        }

        public ReportsCalendarRecyclerAdapter(ReportsDateRange dateRangeParameter)
        {
            DateRangeParameter = dateRangeParameter;
        }

        protected override BaseRecyclerViewHolder<ReportsCalendarDayViewModel> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            var calendarDayCellViewHolder = new CalendarDayCellViewHolder(parent.Context);
            var layoutParams = new RecyclerView.LayoutParams(parent.LayoutParameters);
            layoutParams.Width = itemWidth;
            layoutParams.Height = 51.DpToPixels(parent.Context);
            calendarDayCellViewHolder.ItemView.LayoutParameters = layoutParams;
            return calendarDayCellViewHolder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);
            (holder as CalendarDayCellViewHolder)?.UpdateSelectionState(DateRangeParameter);
        }

        public void UpdateDateRangeParameter(ReportsDateRange newDateRange)
        {
            DateRangeParameter = newDateRange;
            NotifyDataSetChanged();
        }
    }
}
