using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Core.UI.Navigation;
using Toggl.Core.Interactors;
using Toggl.Core.Services;
using Toggl.Core.UI.Parameters;
using Toggl.Shared;
using Toggl.Shared.Extensions;

namespace Toggl.Core.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectWorkspaceViewModel : ViewModel<SelectWorkspaceParameters, long>
    {
        private readonly IInteractorFactory interactorFactory;

        private long currentWorkspaceId;

        public string Title { get; private set; }
        public ReadOnlyCollection<SelectableWorkspaceViewModel> Workspaces { get; private set; }

        public UIAction Close { get; }
        public InputAction<SelectableWorkspaceViewModel> SelectWorkspace { get; }

        public SelectWorkspaceViewModel(
            IInteractorFactory interactorFactory,
            INavigationService navigationService,
            IRxActionFactory rxActionFactory)
            : base(navigationService)
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.interactorFactory = interactorFactory;

            Close = rxActionFactory.FromAsync(close);
            SelectWorkspace = rxActionFactory.FromAsync<SelectableWorkspaceViewModel>(selectWorkspace);
        }

        public override async Task Initialize(SelectWorkspaceParameters parameter)
        {
            base.Initialize(parameter);

            Title = parameter.Title;
            currentWorkspaceId = parameter.CurrentWorkspaceId;

            var workspaces = await interactorFactory.GetAllWorkspaces().Execute();

            Workspaces = workspaces
                .Where(w => w.IsEligibleForProjectCreation())
                .Select(w => new SelectableWorkspaceViewModel(w, w.Id == currentWorkspaceId))
                .ToList()
                .AsReadOnly();
        }

        private Task close()
            => Finish(currentWorkspaceId);

        private Task selectWorkspace(SelectableWorkspaceViewModel workspace)
            => Finish(workspace.WorkspaceId);
    }
}
