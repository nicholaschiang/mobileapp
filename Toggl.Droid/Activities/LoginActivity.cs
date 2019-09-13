using Android.App;
using Android.Content.PM;
using Android.Gms.Auth.Api;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Android.OS;
using Android.Runtime;
using Android.Views;
using System;
using System.Reactive.Linq;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Extensions;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.Presentation;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using static Android.Gms.Common.Apis.GoogleApiClient;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
              ScreenOrientation = ScreenOrientation.Portrait,
              WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class LoginActivity : ReactiveActivity<LoginViewModel>
    {
        public LoginActivity() : base(
            Resource.Layout.LoginActivity,
            Resource.Style.AppTheme_Light,
            Transitions.SlideInFromBottom)
        { }

        public LoginActivity(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        protected override void InitializeBindings()
        {
            ViewModel.Email.FirstAsync()
                .SubscribeOn(AndroidDependencyContainer.Instance.SchedulerProvider.MainScheduler)
                .Subscribe(emailEditText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Password.FirstAsync()
                .SubscribeOn(AndroidDependencyContainer.Instance.SchedulerProvider.MainScheduler)
                .Subscribe(passwordEditText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            //Text
            ViewModel.ErrorMessage
                .Subscribe(errorTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            emailEditText.Rx().Text()
                .Select(Email.From)
                .Subscribe(ViewModel.SetEmail)
                .DisposedBy(DisposeBag);

            passwordEditText.Rx().Text()
                .Select(Password.From)
                .Subscribe(ViewModel.SetPassword)
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Select(loginButtonTitle)
                .Subscribe(loginButton.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.HasError
                .Subscribe(errorTextView.Rx().IsVisible(useGone: false))
                .DisposedBy(DisposeBag);

            ViewModel.IsLoading
                .Subscribe(progressBar.Rx().IsVisible(useGone: false))
                .DisposedBy(DisposeBag);

            ViewModel.LoginEnabled
                .Subscribe(loginButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            //Commands
            signupCard.Rx()
                .BindAction(ViewModel.Signup)
                .DisposedBy(DisposeBag);

            loginButton.Rx().Tap()
                .Subscribe(ViewModel.Login)
                .DisposedBy(DisposeBag);

            passwordEditText.Rx().EditorActionSent()
                .Subscribe(ViewModel.Login)
                .DisposedBy(DisposeBag);

            googleLoginButton.Rx().Tap()
                .Subscribe(ViewModel.GoogleLogin)
                .DisposedBy(DisposeBag);

            forgotPasswordView.Rx()
                .BindAction(ViewModel.ForgotPassword)
                .DisposedBy(DisposeBag);

            string loginButtonTitle(bool isLoading)
                => isLoading ? "" : Shared.Resources.LoginTitle;

            signOutOfGoogle();

            this.CancelAllNotifications();
        }

        private void signOutOfGoogle()
        {
            try
            {
                var signInOptions = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                       .RequestIdToken("{TOGGL_DROID_GOOGLE_SERVICES_CLIENT_ID}")
                       .RequestEmail()
                       .Build();

                var client = new GoogleApiClient.Builder(Application.Context)
                    .AddApi(Auth.GOOGLE_SIGN_IN_API, signInOptions)
                    .Build();

                client.Connect();
                client.RegisterConnectionCallbacks(
                    new ConnectionListener(
                        () => Auth.GoogleSignInApi.SignOut(client)));
            }
            catch (Exception ex)
            {
            }
        }

        class ConnectionListener : Java.Lang.Object, IConnectionCallbacks
        {
            private Action onConnected;

            public ConnectionListener(Action onConnected) : base()
            {
                this.onConnected = onConnected;
            }

            public void OnConnected(Bundle connectionHint)
            {
                onConnected();
            }

            public void OnConnectionSuspended(int cause) { }
        }
    }
}
