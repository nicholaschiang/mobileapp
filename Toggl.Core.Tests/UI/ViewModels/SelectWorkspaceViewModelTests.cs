using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.UI.ViewModels;
using Toggl.Core.Tests.Generators;
using Toggl.Core.Tests.TestExtensions;
using Toggl.Core.UI.Parameters;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Xunit;

namespace Toggl.Core.Tests.UI.ViewModels
{
    public sealed class SelectWorkspaceViewModelTests
    {
        public abstract class SelectWorkspaceViewModelTest : BaseViewModelTests<SelectWorkspaceViewModel, SelectWorkspaceParameters, long>
        {
            protected override SelectWorkspaceViewModel CreateViewModel()
                => new SelectWorkspaceViewModel(InteractorFactory, NavigationService, RxActionFactory);

            protected List<IThreadSafeWorkspace> GenerateWorkspaceList() =>
                Enumerable.Range(0, 10).Select(i =>
                {
                    var workspace = Substitute.For<IThreadSafeWorkspace>();
                    workspace.Id.Returns(i);
                    workspace.Name.Returns(i.ToString());
                    workspace.OnlyAdminsMayCreateProjects.Returns(i < 5);
                    return workspace;
                }).ToList();
        }

        public sealed class TheConstructor : SelectWorkspaceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectWorkspaceViewModel(interactorFactory, navigationService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : SelectWorkspaceViewModelTest
        {
            public ThePrepareMethod()
            {
                var workspaces = GenerateWorkspaceList();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheDefaultWorkspaceId()
            {
                const long expectedId = 8;

                await ViewModel.Initialize(new SelectWorkspaceParameters(string.Empty, expectedId));

                ViewModel.Workspaces.Single(x => x.Selected).WorkspaceId.Should().Be(expectedId);
            }
        }

        public sealed class TheTitleProperty : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task HasCorrectValue()
            {
                var title = "some title";

                await ViewModel.Initialize(new SelectWorkspaceParameters(title, 0));
                ViewModel.Title.Should().Be(title);
            }
        }

        public sealed class TheInitializeMethod : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsEligibleWorkspacesToTheList()
            {
                var workspaces = GenerateWorkspaceList();
                var eligibleWorkspaces = workspaces.Where(ws => ws.IsEligibleForProjectCreation());

                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));

                await ViewModel.Initialize(new SelectWorkspaceParameters("Some workspace", 1));

                ViewModel.Workspaces.Should().HaveCount(eligibleWorkspaces.Count());
            }
        }

        public sealed class TheCloseAction : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize(new SelectWorkspaceParameters("Some workspace", 1));

                ViewModel.Close.Execute();

                await View.Received().Close();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspacePassedOnPrepare()
            {
                const long expectedId = 10;
                await ViewModel.Initialize(new SelectWorkspaceParameters(string.Empty, expectedId));

                ViewModel.Close.Execute();

                (await ViewModel.ReturnedValue()).Should().Be(expectedId);
            }
        }

        public sealed class TheSelectWorkspaceAction : SelectWorkspaceViewModelTest
        {
            private readonly IThreadSafeWorkspace Workspace = Substitute.For<IThreadSafeWorkspace>();

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var selectableWorkspace = new SelectableWorkspaceViewModel(Workspace, true);

                ViewModel.SelectWorkspace.Execute(selectableWorkspace);

                await View.Received().Close();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedWorkspaceId()
            {
                const long expectedId = 10;
                Workspace.Id.Returns(expectedId);
                Workspace.IsEligibleForProjectCreation().Returns(true);
                var selectableWorkspace = new SelectableWorkspaceViewModel(Workspace, true);

                ViewModel.SelectWorkspace.Execute(selectableWorkspace);

                (await ViewModel.ReturnedValue()).Should().Be(expectedId);
            }
        }
    }
}
