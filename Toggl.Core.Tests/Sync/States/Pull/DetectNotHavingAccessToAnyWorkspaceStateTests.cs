﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Core.Sync.States;
using Toggl.Core.Tests.Mocks;
using Toggl.Shared.Models;
using Xunit;
using Toggl.Core.Exceptions;
using Toggl.Core.Sync.States.Pull;
using Toggl.Ultrawave.Exceptions;
using Toggl.Core.DataSources;
using Toggl.Shared;
using Toggl.Core.Models.Interfaces;

namespace Toggl.Core.Tests.Sync.States.Pull
{
    public sealed class DetectNotHavingAccessToAnyWorkspaceStateTests
    {
        private readonly IFetchObservables fetchObservables = Substitute.For<IFetchObservables>();

        private readonly ITogglDataSource dataSource = Substitute.For<ITogglDataSource>();

        private readonly DetectNotHavingAccessToAnyWorkspaceState state;

        public DetectNotHavingAccessToAnyWorkspaceStateTests()
        {
            state = new DetectNotHavingAccessToAnyWorkspaceState(dataSource);
        }

        [Fact]
        public async Task ReturnsSuccessResultWhenWorkspacesArePresent()
        {
            var arrayWithWorkspace = new List<IThreadSafeWorkspace>(new[] { new MockWorkspace() });
            dataSource.Workspaces.GetAll().Returns(Observable.Return(arrayWithWorkspace));

            var transition = await state.Start(fetchObservables);

            transition.Result.Should().Be(state.Done);
        }

        [Fact, LogIfTooSlow]
        public void ThrowsExceptionsWhenNoWorkspacesAreAvailableInTheDatabaseAfterPullingWorspaces()
        {
            var arrayWithNoWorkspace = new List<IThreadSafeWorkspace>();
            dataSource.Workspaces.GetAll().Returns(Observable.Return(arrayWithNoWorkspace));

            Func<Task> fetchWorkspaces = async () => await state.Start(fetchObservables);

            fetchWorkspaces.Should().Throw<NoWorkspaceException>();
        }
    }
}
