using System;
using System.Reactive.Linq;
using Toggl.Core.Analytics;
using Toggl.Core.DataSources;
using Toggl.Core.Extensions;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage;
using Toggl.Core.Services;
using Toggl.Core.Sync;

namespace Toggl.Core.Interactors
{
    internal sealed class CreateTimeEntryInteractor : IInteractor<IObservable<IThreadSafeTimeEntry>>
    {
        private readonly TimeSpan? duration;
        private readonly IIdProvider idProvider;
        private readonly DateTimeOffset startTime;
        private readonly ITimeService timeService;
        private readonly TimeEntryStartOrigin origin;
        private readonly ITogglDataSource dataSource;
        private readonly ITimeEntryPrototype prototype;
        private readonly IAnalyticsService analyticsService;
        private readonly ISyncManager syncManager;

        public CreateTimeEntryInteractor(
            IIdProvider idProvider,
            ITimeService timeService,
            ITogglDataSource dataSource,
            IAnalyticsService analyticsService,
            ITimeEntryPrototype prototype,
            ISyncManager syncManager,
            DateTimeOffset startTime,
            TimeSpan? duration,
            TimeEntryStartOrigin origin)
        {
            Ensure.Argument.IsNotNull(origin, nameof(origin));
            Ensure.Argument.IsNotNull(prototype, nameof(prototype));
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));

            this.origin = origin;
            this.duration = duration;
            this.prototype = prototype;
            this.startTime = startTime;
            this.idProvider = idProvider;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.analyticsService = analyticsService;
            this.syncManager = syncManager;
        }

        public IObservable<IThreadSafeTimeEntry> Execute()
            => dataSource.User.Current
                .FirstAsync()
                .Select(userFromPrototype)
                .SelectMany(dataSource.TimeEntries.Create)
                .Do(notifyOfNewTimeEntryIfPossible)
                .Do(syncManager.InitiatePushSync)
                .Track(StartTimeEntryEvent.With(origin), analyticsService);

        private TimeEntry userFromPrototype(IThreadSafeUser user)
            => idProvider.GetNextIdentifier()
                .Apply(TimeEntry.Builder.Create)
                .SetUserId(user.Id)
                .SetTagIds(prototype.TagIds)
                .SetTaskId(prototype.TaskId)
                .SetStart(startTime)
                .SetDuration(duration)
                .SetBillable(prototype.IsBillable)
                .SetProjectId(prototype.ProjectId)
                .SetDescription(prototype.Description)
                .SetWorkspaceId(prototype.WorkspaceId)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build();

        private void notifyOfNewTimeEntryIfPossible(IThreadSafeTimeEntry timeEntry)
        {
            if (dataSource.TimeEntries is TimeEntriesDataSource timeEntriesDataSource)
                timeEntriesDataSource.OnTimeEntryStarted(timeEntry, origin);
        }
    }
}
