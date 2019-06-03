using System;
using System.Reactive.Linq;
using CoreText;
using Foundation;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.Core;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Helper;
using Toggl.Core.UI.ViewModels;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public partial class TokenResetViewController : KeyboardAwareViewController<TokenResetViewModel>
    {
        private const int forgotPasswordLabelOffset = 27;

        private readonly UIBarButtonItem nextButton =
            new UIBarButtonItem { Title = Resources.LoginNextButton, TintColor = UIColor.White };

        public TokenResetViewController(TokenResetViewModel viewModel)
            : base(viewModel, nameof(TokenResetViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.LoginTitle;
            ResetSuccessLabel.Text = Resources.APITokenResetSuccess;
            InstructionLabel.Text = Resources.TokenResetInstruction;
            PasswordTextField.Placeholder = Resources.Password;
            SignOutButton.SetTitle(Resources.OrSignOut, UIControlState.Normal);

            prepareViews();

            EmailLabel.Text = ViewModel.Email.ToString();

            ViewModel.Error
                .Subscribe(ErrorLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.Password
                .Subscribe(PasswordTextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx().Text()
                .Subscribe(ViewModel.Password)
                .DisposedBy(DisposeBag);

            SignOutButton.Rx()
                .BindAction(ViewModel.SignOut)
                .DisposedBy(DisposeBag);

            ViewModel.SignOut.Elements
                .Subscribe(IosDependencyContainer.Instance.IntentDonationService.ClearAll)
                .DisposedBy(DisposeBag);

            ShowPasswordButton.Rx().Tap()
                .Subscribe(_ =>
                {
                    PasswordTextField.ResignFirstResponder();
                    PasswordTextField.SecureTextEntry = !PasswordTextField.SecureTextEntry;
                    PasswordTextField.BecomeFirstResponder();
                })
                .DisposedBy(DisposeBag);

            nextButton.Rx().Tap()
                .Subscribe(ViewModel.Done.Inputs)
                .DisposedBy(DisposeBag);

            PasswordTextField.Rx().ShouldReturn()
                .Subscribe(ViewModel.Done.Inputs)
                .DisposedBy(DisposeBag);

            //Enabled
            ViewModel.NextIsEnabled
                .Subscribe(nextButton.Rx().Enabled())
                .DisposedBy(DisposeBag);

            //Visibility
            ViewModel.HasError
                .Subscribe(ErrorView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Done.Executing
                .Invert()
                .Subscribe(ShowPasswordButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.Done.Executing
                .Subscribe(ActivityIndicatorView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            PasswordTextField.BecomeFirstResponder();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationController.NavigationBar.UserInteractionEnabled = true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = e.FrameEnd.Height + forgotPasswordLabelOffset;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = forgotPasswordLabelOffset;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            prepareTextFields();
            prepareNavigationBar();
        }

        private void prepareTextFields()
        {
            var placeholderAttributes = new CTStringAttributes(
                new UIStringAttributes { ForegroundColor = UIColor.White.ColorWithAlpha(0.5f) }.Dictionary
            );

            PasswordTextField.TintColor = UIColor.White;
            PasswordTextField.AttributedPlaceholder =
                new NSAttributedString(Resources.LoginSignUpPasswordPlaceholder, placeholderAttributes);
        }

        private void prepareNavigationBar()
        {
            var attributes = new UITextAttributes { Font = UIFont.SystemFontOfSize(14, UIFontWeight.Medium) };
            var spaceFix = new UIBarButtonItem(UIBarButtonSystemItem.FixedSpace) { Width = 8 };

            NavigationItem.RightBarButtonItems = new[] { spaceFix, nextButton };

            nextButton.SetTitleTextAttributes(attributes, UIControlState.Normal);
            nextButton.SetTitleTextAttributes(attributes, UIControlState.Disabled);
        }
    }
}

