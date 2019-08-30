using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.UI.Collections;
using Toggl.Core.UI.Extensions;
using Toggl.Core.UI.ViewModels;
using Toggl.iOS.Extensions;
using Toggl.iOS.Extensions.Reactive;
using Toggl.iOS.Views;
using Toggl.iOS.ViewSources.Generic.TableView;
using Toggl.Shared.Extensions;
using UIKit;

namespace Toggl.iOS.ViewControllers
{
    public partial class SelectWorkspaceViewController : ReactiveViewController<SelectWorkspaceViewModel>
    {
        private const int rowHeight = 64;
        private const double headerHeight = 54;

        public SelectWorkspaceViewController(SelectWorkspaceViewModel viewModel)
            : base(viewModel, nameof(SelectWorkspaceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.BackgroundColor = ColorAssets.Background;
            Separator.BackgroundColor = ColorAssets.Table.Separator;
            CloseButton.SetImage(
                CloseButton.ImageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                UIControlState.Normal
                );
            CloseButton.TintColor = ColorAssets.Navigation.BarButtons;

            WorkspaceTableView.RowHeight = rowHeight;
            WorkspaceTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            WorkspaceTableView.RegisterNibForCellReuse(WorkspaceViewCell.Nib, WorkspaceViewCell.Identifier);

            var source = new CustomTableViewSource<SectionModel<Unit, SelectableWorkspaceViewModel>, Unit, SelectableWorkspaceViewModel>(
                WorkspaceViewCell.CellConfiguration(WorkspaceViewCell.Identifier),
                ViewModel.Workspaces
            );
            WorkspaceTableView.Source = source;

            TitleLabel.Text = ViewModel.Title;

            CloseButton.Rx().Tap()
                .Subscribe(ViewModel.CloseWithDefaultResult)
                .DisposedBy(DisposeBag);

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectWorkspace.Inputs)
                .DisposedBy(DisposeBag);

            if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad)
                PreferredContentSize = new CoreGraphics.CGSize(0, headerHeight + (ViewModel.Workspaces.Count * rowHeight));
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            View.ClipsToBounds |= UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
        }

        public override void ViewWillLayoutSubviews()
        {
            base.ViewWillLayoutSubviews();
            View.ClipsToBounds |= UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
        }
    }
}

