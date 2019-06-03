using System;
using System.Reactive;
using System.Reactive.Concurrency;
using FluentAssertions;
using NSubstitute;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Sync;
using Toggl.Storage;
using Toggl.Networking;
using Toggl.Storage.Settings;
using Xunit;

namespace Toggl.Core.Tests.Sync
{
    public sealed class SyncGraphTests
    {
        public sealed class TheSyncGraph
        {
            [Fact, LogIfTooSlow]
            public void HasNoLooseEnds()
            {
                var configurator = configureTogglSyncGraph();

                var looseEnds = configurator.GetAllLooseEndStateResults();

                looseEnds.Should().BeEmpty();
            }
        }

        private static TestConfigurator configureTogglSyncGraph()
        {
            var configurator = new TestConfigurator();
            var entryPoints = new StateMachineEntryPoints();

            configurator.AllDistinctStatesInOrder.Add(entryPoints);

            TogglSyncManager.ConfigureTransitions(
                configurator,
                Substitute.For<ITogglDatabase>(),
                Substitute.For<ITogglApi>(),
                Substitute.For<ITogglDataSource>(),
                Substitute.For<IScheduler>(),
                Substitute.For<ITimeService>(),
                Substitute.For<IAnalyticsService>(),
                Substitute.For<ILastTimeUsageStorage>(),
                entryPoints,
                Substitute.For<ISyncStateQueue>()
            );

            return configurator;
        }
    }
}
