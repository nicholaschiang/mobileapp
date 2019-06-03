using System;
using System.Reactive.Linq;
using Toggl.Core.Interactors;
using Toggl.Shared;
using Toggl.Storage.Settings;

namespace Toggl.Core.Sync.States.PullTimeEntries
{
    public sealed class DetectPlaceholdersWereCreatedState : ISyncState
    {
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ITimeService timeService;
        private readonly Func<IInteractor<IObservable<bool>>> containsPlaceholdersFactory;

        public StateResult Done { get; } = new StateResult();

        public DetectPlaceholdersWereCreatedState(
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            Func<IInteractor<IObservable<bool>>> containsPlaceholdersFactory)
        {
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(containsPlaceholdersFactory, nameof(containsPlaceholdersFactory));

            this.lastTimeUsageStorage = lastTimeUsageStorage;
            this.timeService = timeService;
            this.containsPlaceholdersFactory = containsPlaceholdersFactory;
        }

        public IObservable<ITransition> Start()
            => containsPlaceholdersFactory()
                .Execute()
                .Select(containsPlaceholders =>
                {
                    if (containsPlaceholders)
                    {
                        lastTimeUsageStorage.SetPlaceholdersWereCreated(timeService.CurrentDateTime);
                    }

                    return Done.Transition();
                });
    }
}
