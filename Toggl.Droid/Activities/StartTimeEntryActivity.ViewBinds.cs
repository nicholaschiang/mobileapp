using Android.Support.V7.Widget;
using Android.Widget;
using Toggl.Droid.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using static Toggl.Droid.Resource.Id;
using Android.Views;

namespace Toggl.Droid.Activities
{
    public partial class StartTimeEntryActivity
    {
        private TextView doneButton;
        private ImageView closeButton;
        private ImageView selectTagToolbarButton;
        private ImageView selectProjectToolbarButton;
        private ImageView selectBillableToolbarButton;

        private TextView durationLabel;

        private RecyclerView recyclerView;

        private AutocompleteEditText descriptionField;

        private View toolbar;

        protected override void InitializeViews()
        {
            closeButton = FindViewById<ImageView>(CloseButton);
            selectTagToolbarButton = FindViewById<ImageView>(ToolbarTagButton);
            selectProjectToolbarButton = FindViewById<ImageView>(ToolbarProjectButton);
            selectBillableToolbarButton = FindViewById<ImageView>(ToolbarBillableButton);

            doneButton = FindViewById<TextView>(DoneButton);
            durationLabel = FindViewById<TextView>(DurationText);

            recyclerView = FindViewById<RecyclerView>(SuggestionsRecyclerView);

            descriptionField = FindViewById<AutocompleteEditText>(DescriptionTextField);

            toolbar = FindViewById(Resource.Id.Toolbar);
        }
    }
}
