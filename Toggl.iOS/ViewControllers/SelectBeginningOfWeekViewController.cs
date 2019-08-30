using System.Reactive;
using System.Threading.Tasks;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.Views.Settings;
using Toggl.iOS.ViewSources.Generic.TableView;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public partial class SelectBeginningOfWeekViewController : ReactiveViewController<SelectBeginningOfWeekViewModel>
    {
        public SelectBeginningOfWeekViewController(SelectBeginningOfWeekViewModel viewModel)
            : base(viewModel, nameof(SelectBeginningOfWeekViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = ColorAssets.Background;
            BackButton.SetImage(
                BackButton.ImageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                UIControlState.Normal
            );
            BackButton.TintColor = ColorAssets.Navigation.BarButtons;

            TitleLabel.Text = Resources.FirstDayOfTheWeek;

            DaysTableView.RegisterNibForCellReuse(DayOfWeekViewCell.Nib, DayOfWeekViewCell.Identifier);

            var source = new CustomTableViewSource<SectionModel<Unit, SelectableBeginningOfWeekViewModel>, Unit, SelectableBeginningOfWeekViewModel>(
                DayOfWeekViewCell.CellConfiguration(DayOfWeekViewCell.Identifier),
                ViewModel.BeginningOfWeekCollection
            );

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectBeginningOfWeek.Inputs)
                .DisposedBy(DisposeBag);

            DaysTableView.Source = source;

            BackButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);
        }
    }
}

