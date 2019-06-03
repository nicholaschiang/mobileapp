using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Core.Models;
using Toggl.Core.Sync;

namespace Toggl.Core.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<SyncFailureItem>>> GetItemsThatFailedToSync()
            => new GetItemsThatFailedToSyncInteractor(dataSource);

        public IInteractor<IObservable<bool>> HasFinishedSyncBefore()
            => new HasFinsihedSyncBeforeInteractor(dataSource);

        public IInteractor<IObservable<SyncOutcome>> RunBackgroundSync()
            => new RunBackgroundSyncInteractor(syncManager, analyticsService, stopwatchProvider);

        public IInteractor<IObservable<bool>> ContainsPlaceholders()
            => new ContainsPlaceholdersInteractor(dataSource);
    }
}
