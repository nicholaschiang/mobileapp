using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Adapters;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class SelectProjectActivity : ReactiveActivity<SelectProjectViewModel>
    {
        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_BlueStatusBar_WhiteBackground);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.SelectProjectActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);
            InitializeViews();

            var adapter = new SelectProjectRecyclerAdapter();
            recyclerView.SetLayoutManager(new LinearLayoutManager(this));
            recyclerView.SetAdapter(adapter);

            ViewModel.Suggestions
                .Subscribe(adapter.Rx().Items())
                .DisposedBy(DisposeBag);

            adapter.ItemsUpdateCompleted
                .Subscribe(scrollToTop)
                .DisposedBy(DisposeBag);

            adapter.ItemTapObservable
                .Subscribe(ViewModel.SelectProject.Inputs)
                .DisposedBy(DisposeBag);

            adapter.ToggleTasks
                .Subscribe(ViewModel.ToggleTaskSuggestions.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.PlaceholderText
                .Subscribe(searchField.Rx().Hint())
                .DisposedBy(DisposeBag);

            searchField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            closeButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);
        }

        private void scrollToTop(Unit _)
        {
            recyclerView.GetLayoutManager().ScrollToPosition(0);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }
    }
}
