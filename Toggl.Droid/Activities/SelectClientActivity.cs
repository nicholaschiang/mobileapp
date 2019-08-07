using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using System;
using System.Reactive.Linq;
using System.Linq;
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
    public partial class SelectClientActivity : ReactiveActivity<SelectClientViewModel>
    {
        private SelectClientRecyclerAdapter selectClientRecyclerAdapter = new SelectClientRecyclerAdapter();

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light_WhiteBackground);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.SelectClientActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupLayoutManager(selectClientRecyclerAdapter);

            ViewModel.Clients
                .Select(clients => clients.ToList())
                .Subscribe(selectClientRecyclerAdapter.Rx().Items())
                .DisposedBy(DisposeBag);

            backImageView.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            filterEditText.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            selectClientRecyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectClient.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void setupLayoutManager(SelectClientRecyclerAdapter adapter)
        {
            var layoutManager = new LinearLayoutManager(this)
            {
                ItemPrefetchEnabled = true,
                InitialPrefetchItemCount = 4
            };
            selectClientRecyclerView.SetLayoutManager(layoutManager);
            selectClientRecyclerView.SetAdapter(adapter);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }
    }
}
