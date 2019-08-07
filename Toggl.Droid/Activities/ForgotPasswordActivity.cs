using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
        ScreenOrientation = ScreenOrientation.Portrait,
        WindowSoftInputMode = SoftInput.StateVisible,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class ForgotPasswordActivity : ReactiveActivity<ForgotPasswordViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.ForgotPasswordActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            setupToolbar();
            InitializeViews();
            setupInputField();

            ViewModel.ErrorMessage
                .Subscribe(errorMessage =>
                {
                    loginEmail.Error = errorMessage;
                })
                .DisposedBy(DisposeBag);

            loginEmailEditText.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.Email.OnNext)
                .DisposedBy(DisposeBag);

            ViewModel.Reset.Executing
                .Subscribe(loadingProgressBar.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordResetSuccessful
                .Where(success => success)
                .Subscribe(_ => showResetPasswordSuccessToast())
                .DisposedBy(DisposeBag);

            ViewModel.PasswordResetSuccessful
                .Invert()
                .Subscribe(resetPasswordButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            resetPasswordButton.Rx()
                .BindAction(ViewModel.Reset)
                .DisposedBy(DisposeBag);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void setupInputField()
        {
            loginEmailEditText.SetFocus();
            loginEmailEditText.SetSelection(loginEmailEditText.Text?.Length ?? 0);
        }

        private void showResetPasswordSuccessToast()
        {
            loginEmailEditText.RemoveFocus();
            Toast.MakeText(this, Resource.String.ResetPasswordEmailSentMessage, ToastLength.Long).Show();
        }

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.ForgotPasswordToolbar);

            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.Title = GetString(Resource.String.ForgotPasswordTitle);
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
    }
}
