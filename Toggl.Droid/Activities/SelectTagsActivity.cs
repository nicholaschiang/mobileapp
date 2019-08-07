using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using System;
using System.Linq;
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
    public partial class SelectTagsActivity : ReactiveActivity<SelectTagsViewModel>
    {
        private SelectTagsRecyclerAdapter selectTagsRecyclerAdapter = new SelectTagsRecyclerAdapter();

        protected override void OnCreate(Bundle bundle)
        {
            SetTheme(Resource.Style.AppTheme_Light_WhiteBackground);
            base.OnCreate(bundle);
            if (ViewModelWasNotCached())
            {
                BailOutToSplashScreen();
                return;
            }
            SetContentView(Resource.Layout.SelectTagsActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupLayoutManager(selectTagsRecyclerAdapter);

            ViewModel.Tags
                .Select(tags => tags.ToList())
                .Subscribe(selectTagsRecyclerAdapter.Rx().Items())
                .DisposedBy(DisposeBag);

            ViewModel.FilterText
                .Select(text => text == string.Empty)
                .DistinctUntilChanged()
                .Subscribe(saveButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.FilterText
                .Select(text => text == string.Empty)
                .DistinctUntilChanged()
                .Invert()
                .Subscribe(clearIcon.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            backIcon.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            clearIcon.Click += (sender, e) =>
            {
                textField.Text = string.Empty;
            };

            saveButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            textField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            selectTagsRecyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectTag.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void setupLayoutManager(SelectTagsRecyclerAdapter adapter)
        {
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            selectTagsRecyclerView.SetLayoutManager(layoutManager);
            selectTagsRecyclerView.SetAdapter(adapter);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }
    }
}
