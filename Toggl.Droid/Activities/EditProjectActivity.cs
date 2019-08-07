using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.Graphics;
using Android.Views;
using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Shared.Extensions;
using AndroidColor = Android.Graphics.Color;
using FoundationResources = Toggl.Shared.Resources;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
              WindowSoftInputMode = SoftInput.AdjustResize,
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class EditProjectActivity : ReactiveActivity<EditProjectViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light_WhiteBackground);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.EditProjectActivity);
            InitializeViews();
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);
            setupToolbar();
            errorText.Visibility = ViewStates.Gone;

            // Name
            projectNameTextView.Rx()
                .Text()
                .Subscribe(ViewModel.Name.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.Name
                .Subscribe(projectNameTextView.Rx().TextObserver(ignoreUnchanged: true))
                .DisposedBy(DisposeBag);

            // Color
            colorCircle.Rx()
                .BindAction(ViewModel.PickColor)
                .DisposedBy(DisposeBag);

            colorArrow.Rx()
                .BindAction(ViewModel.PickColor)
                .DisposedBy(DisposeBag);

            ViewModel.Color
                .Select(color => color.ToNativeColor())
                .Subscribe(colorCircle.SetCircleColor)
                .DisposedBy(DisposeBag);
                
            // Error
            ViewModel.Error
                .Subscribe(errorText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Error
                .Select(e => !string.IsNullOrEmpty(e))
                .Subscribe(errorText.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            var errorOffset = 8.DpToPixels(this);
            var noErrorOffset = 14.DpToPixels(this);

            ViewModel.Error
                .Select(e => string.IsNullOrEmpty(e) ? noErrorOffset : errorOffset)
                .Subscribe(projectNameTextView.LayoutParameters.Rx().MarginTop())
                .DisposedBy(DisposeBag);
            // Workspace
            changeWorkspaceView.Rx()
                .BindAction(ViewModel.PickWorkspace)
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceName
                .Subscribe(workspaceNameLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            // Client
            changeClientView.Rx()
                .BindAction(ViewModel.PickClient)
                .DisposedBy(DisposeBag);

            ViewModel.ClientName
                .Select(clientNameWithEmptyText)
                .Subscribe(clientNameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            var noClientColor = AndroidColor.ParseColor("#CECECE");
            ViewModel.ClientName
                .Select(clientTextColor)
                .Subscribe(clientNameTextView.SetTextColor)
                .DisposedBy(DisposeBag);

            // Is Private
            toggleIsPrivateView.Rx().Tap()
                .Select(_ => isPrivateSwitch.Checked)
                .Subscribe(ViewModel.IsPrivate.Accept)
                .DisposedBy(DisposeBag);

            ViewModel.IsPrivate
                .Subscribe(isPrivateSwitch.Rx().CheckedObserver())
                .DisposedBy(DisposeBag);

            // Save
            createProjectButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            var enabledColor = AndroidColor.White;
            var disabledColor = new AndroidColor(ColorUtils.SetAlphaComponent(AndroidColor.White, 127));
            ViewModel.Save.Enabled
                .Select(createProjectTextColor)
                .Subscribe(createProjectButton.SetTextColor)
                .DisposedBy(DisposeBag);

            string clientNameWithEmptyText(string clientName)
                => string.IsNullOrEmpty(clientName) ? FoundationResources.AddClient : clientName;

            AndroidColor clientTextColor(string clientName)
                => string.IsNullOrEmpty(clientName) ? noClientColor : AndroidColor.Black;

            AndroidColor createProjectTextColor(bool enabled)
                => enabled ? enabledColor : disabledColor;
        }


        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                ViewModel.CloseWithDefaultResult();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void setupToolbar()
        {
            toolbar.Title = ViewModel.Title;

            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
        }
    }
}
