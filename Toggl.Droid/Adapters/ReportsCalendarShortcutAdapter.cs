using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Core.UI.Parameters;
using Toggl.Core.UI.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Droid.ViewHolders;

namespace Toggl.Droid.Adapters
{
    public class ReportsCalendarShortcutAdapter : BaseRecyclerAdapter<ReportsCalendarBaseQuickSelectShortcut>
    {
        private ReportsDateRange currentDateRange;

        protected override BaseRecyclerViewHolder<ReportsCalendarBaseQuickSelectShortcut> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            var view = inflater.Inflate(Resource.Layout.ReportsCalendarShortcutCell, parent, false);
            return ReportsCalendarShortcutCellViewHolder.Create(view);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            base.OnBindViewHolder(holder, position);
            (holder as ReportsCalendarShortcutCellViewHolder)?.UpdateSelectionState(currentDateRange);
        }

        public void UpdateSelectedShortcut(ReportsDateRange newDateRange)
        {
            currentDateRange = newDateRange;
            NotifyDataSetChanged();
        }
    }
}
