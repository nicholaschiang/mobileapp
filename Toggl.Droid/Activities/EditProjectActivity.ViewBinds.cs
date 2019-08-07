using Android.Views;
using Android.Widget;
using Toggl.Droid.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Droid.Activities
{
    public sealed partial class EditProjectActivity
    {
        private TextView errorText;
        private View colorArrow;
        private View changeClientView;
        private View changeWorkspaceView;
        private View toggleIsPrivateView;

        private TextView workspaceNameLabel;
        private TextView clientNameTextView;
        private TextView createProjectButton;
        private TextView projectNameTextView;

        private Switch isPrivateSwitch;

        private CircleView colorCircle;

        private Toolbar toolbar;

        protected override void InitializeViews()
        {
            errorText = FindViewById<TextView>(Resource.Id.ErrorText);
            colorArrow = FindViewById(Resource.Id.ColorArrow);
            changeClientView = FindViewById(Resource.Id.ChangeClientView);
            colorCircle = FindViewById<CircleView>(Resource.Id.ColorCircle);
            changeWorkspaceView = FindViewById(Resource.Id.ChangeWorkspaceView);
            toggleIsPrivateView = FindViewById(Resource.Id.ToggleIsPrivateView);
            isPrivateSwitch = FindViewById<Switch>(Resource.Id.IsPrivateSwitch);
            clientNameTextView = FindViewById<TextView>(Resource.Id.ClientNameTextView);
            workspaceNameLabel = FindViewById<TextView>(Resource.Id.WorkspaceNameLabel);
            createProjectButton = FindViewById<TextView>(Resource.Id.CreateProjectButton);
            projectNameTextView = FindViewById<TextView>(Resource.Id.ProjectNameTextView);
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
