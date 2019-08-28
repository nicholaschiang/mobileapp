using System.Reactive.Disposables;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.UI.ViewModels.Selectable;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.Views.Settings;
using Toggl.iOS.ViewSources.Generic.TableView;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public sealed partial class SelectDateFormatViewController
        : ReactiveViewController<SelectDateFormatViewModel>
    {
        private const int rowHeight = 48;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public SelectDateFormatViewController(SelectDateFormatViewModel viewModel)
            : base(viewModel, nameof(SelectDateFormatViewController))
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
            BackButton.TintColor = ColorAssets.CustomGray2;

            TitleLabel.Text = Resources.DateFormat;

            DateFormatsTableView.RegisterNibForCellReuse(DateFormatViewCell.Nib, DateFormatViewCell.Identifier);
            DateFormatsTableView.RowHeight = rowHeight;

            var source = new CustomTableViewSource<SectionModel<string, SelectableDateFormatViewModel>, string, SelectableDateFormatViewModel>(
                DateFormatViewCell.CellConfiguration(DateFormatViewCell.Identifier),
                ViewModel.DateTimeFormats
            );

            DateFormatsTableView.Source = source;

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectDateFormat.Inputs)
                .DisposedBy(disposeBag);

            BackButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(disposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            disposeBag.Dispose();
        }
    }
}

