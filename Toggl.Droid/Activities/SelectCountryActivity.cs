using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.Droid.Adapters;
using Toggl.Droid.Extensions.Reactive;
using Toggl.Droid.ViewHolders.Country;
using Toggl.Shared.Extensions;

namespace Toggl.Droid.Activities
{
    [Activity(Theme = "@style/Theme.Splash",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public partial class SelectCountryActivity : ReactiveActivity<SelectCountryViewModel>
    {
        private SimpleAdapter<SelectableCountryViewModel> recyclerAdapter =
            new SimpleAdapter<SelectableCountryViewModel>(Resource.Layout.SelectCountryActivityCountryCell,
                CountrySelectionViewHolder.Create);

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.SelectCountryActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupRecyclerView(adapter: recyclerAdapter);

            ViewModel.Countries
                .Subscribe(replaceCountries)
                .DisposedBy(DisposeBag);

            backImageView.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            filterEditText.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            recyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectCountry.Inputs)
                .DisposedBy(DisposeBag);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void setupRecyclerView(SimpleAdapter<SelectableCountryViewModel> adapter)
        {
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);
        }

        private void replaceCountries(IEnumerable<SelectableCountryViewModel> countries)
        {
            recyclerAdapter.Items = countries.ToList();
        }
    }
}
