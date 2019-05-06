﻿using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.Views;
using Toggl.iOS.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public abstract partial class ReactiveViewController<TViewModel> : UIViewController, IView
        where TViewModel : IViewModel
    {
        public CompositeDisposable DisposeBag { get; private set; } = new CompositeDisposable();

        public TViewModel ViewModel { get; set; }

        protected ReactiveViewController(string nibName)
            : base(nibName, null) { }

        protected ReactiveViewController(IntPtr handle)
            : base(handle) { }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            ViewModel?.AttachView(this);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            ViewModel?.ViewAppearing();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            ViewModel?.ViewAppeared();
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            ViewModel?.ViewDisappearing();
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);
            ViewModel?.ViewDisappeared();
        }

        public Task Close()
        {
            this.Dismiss();
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            DisposeBag?.Dispose();
        }
    }
}
