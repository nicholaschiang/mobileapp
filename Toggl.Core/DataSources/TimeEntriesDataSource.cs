using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Generic;
using Toggl.Core.Analytics;
using Toggl.Core.Models;
using Toggl.Shared;
using Toggl.Shared.Extensions;
using Toggl.Storage;
using Toggl.Storage.Models;
using Toggl.Core.Extensions;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Sync.ConflictResolution;
using Toggl.Core.Exceptions;

namespace Toggl.Core.DataSources
{
    internal sealed class TimeEntriesDataSource : ObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry>, ITimeEntriesSource
    {
        private long? currentlyRunningTimeEntryId;
        private readonly ITimeService timeService;
        private readonly IAnalyticsService analyticsService;
        private readonly IRepository<IDatabaseTimeEntry> repository;

        private readonly Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode> alwaysCreate
            = (a, b) => ConflictResolutionMode.Create;

        private readonly Subject<IThreadSafeTimeEntry> timeEntryStartedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> timeEntryStoppedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> suggestionStartedSubject = new Subject<IThreadSafeTimeEntry>();
        private readonly Subject<IThreadSafeTimeEntry> timeEntryContinuedSubject = new Subject<IThreadSafeTimeEntry>();

        public IObservable<IThreadSafeTimeEntry> TimeEntryStarted { get; }
        public IObservable<IThreadSafeTimeEntry> TimeEntryStopped { get; }
        public IObservable<IThreadSafeTimeEntry> SuggestionStarted { get; }
        public IObservable<IThreadSafeTimeEntry> TimeEntryContinued { get; }

        public IObservable<bool> IsEmpty { get; }

        public IObservable<IThreadSafeTimeEntry> CurrentlyRunningTimeEntry { get; }

        protected override IRivalsResolver<IDatabaseTimeEntry> RivalsResolver { get; }

        public TimeEntriesDataSource(
            IRepository<IDatabaseTimeEntry> repository,
            ITimeService timeService,
            IAnalyticsService analyticsService)
            : base(repository)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(repository, nameof(repository));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));

            this.timeService = timeService;
            this.repository = repository;
            this.analyticsService = analyticsService;

            RivalsResolver = new TimeEntryRivalsResolver(timeService);

            CurrentlyRunningTimeEntry =
                getCurrentlyRunningTimeEntry()
                    .StartWith()
                    .Merge(Created.Where(te => te.IsRunning()))
                    .Merge(Updated.Where(update => update.Id == currentlyRunningTimeEntryId).Select(update => update.Entity))
                    .Merge(Deleted.Where(id => id == currentlyRunningTimeEntryId).Select(_ => null as IThreadSafeTimeEntry))
                    .Select(runningTimeEntry)
                    .ConnectedReplay();

            IsEmpty =
                Observable.Return(default(IThreadSafeTimeEntry))
                    .StartWith()
                    .Merge(Updated.Select(tuple => tuple.Entity))
                    .Merge(Created)
                    .SelectMany(_ => GetAll(te => te.IsDeleted == false))
                    .Select(timeEntries => timeEntries.None());

            RivalsResolver = new TimeEntryRivalsResolver(timeService);

            TimeEntryStarted = timeEntryStartedSubject.AsObservable();
            TimeEntryStopped = timeEntryStoppedSubject.AsObservable();
            SuggestionStarted = suggestionStartedSubject.AsObservable();
            TimeEntryContinued = timeEntryContinuedSubject.AsObservable();
        }

        public override IObservable<IThreadSafeTimeEntry> Create(IThreadSafeTimeEntry entity)
            => checkForOutOfBoundsDate(entity, "Create")
                .SelectMany(_ => repository.UpdateWithConflictResolution(entity.Id, entity, alwaysCreate, RivalsResolver)  
                    .ToThreadSafeResult(Convert)
                    .SelectMany(CommonFunctions.Identity)
                    .Do(HandleConflictResolutionResult)
                    .OfType<CreateResult<IThreadSafeTimeEntry>>()
                    .FirstAsync()
                    .Select(result => result.Entity));

        public void OnTimeEntryStopped(IThreadSafeTimeEntry timeEntry)
        {
            timeEntryStoppedSubject.OnNext(timeEntry);
        }

        public void OnTimeEntrySoftDeleted(IThreadSafeTimeEntry timeEntry)
        {
            DeletedSubject.OnNext(timeEntry.Id);
        }

        public void OnTimeEntryStarted(IThreadSafeTimeEntry timeEntry, TimeEntryStartOrigin origin)
        {
            switch (origin)
            {
                case TimeEntryStartOrigin.ContinueMostRecent:
                    timeEntryContinuedSubject.OnNext(timeEntry);
                    break;

                case TimeEntryStartOrigin.Manual:
                case TimeEntryStartOrigin.Timer:
                    timeEntryStartedSubject.OnNext(timeEntry);
                    break;

                case TimeEntryStartOrigin.Suggestion:
                    suggestionStartedSubject.OnNext(timeEntry);
                    break;
            }
        }

        public override IObservable<IEnumerable<IConflictResolutionResult<IThreadSafeTimeEntry>>> BatchUpdate(IEnumerable<IThreadSafeTimeEntry> entities)
            => entities.Select(timeEntry => checkForOutOfBoundsDate(timeEntry, "BatchUpdate"))
                .Merge()
                .LastOrDefaultAsync()
                .SelectMany(_ => base.BatchUpdate(entities));

        public override IObservable<IThreadSafeTimeEntry> Update(IThreadSafeTimeEntry entity)
            => checkForOutOfBoundsDate(entity, "Update")
                .SelectMany(_ => base.Update(entity));

        private IObservable<Unit> checkForOutOfBoundsDate(IThreadSafeTimeEntry timeEntry, string source)
        {
            if (timeEntry.IsDeleted == true)
                return Observable.Return(Unit.Default);

            var newStartTimeIsWithinLimits = timeEntry.Start.IsWithinTogglLimits();
            var newEndTimeIsWithinLimits = timeEntry.StopTime()?.IsWithinTogglLimits() ?? true;

            if (newStartTimeIsWithinLimits && newEndTimeIsWithinLimits)
                return Observable.Return(Unit.Default);

            return GetById(timeEntry.Id)
                .Catch(Observable.Return<IThreadSafeTimeEntry>(null))
                .Do(oldTimeEntry =>
                {
                    var oldStartTimeIsWithinLimits = oldTimeEntry == null
                        ? true
                        : oldTimeEntry.Start.IsWithinTogglLimits();

                    var oldEndTimeIsWithinLimits = oldTimeEntry == null
                        ? true
                        : oldTimeEntry?.StopTime()?.IsWithinTogglLimits() ?? true;

                    if (!newStartTimeIsWithinLimits && oldStartTimeIsWithinLimits)
                        logOutOfBoundsDate(oldTimeEntry.Start, timeEntry.Start, "Start", source);

                    if (!newEndTimeIsWithinLimits && oldEndTimeIsWithinLimits)
                        logOutOfBoundsDate(oldTimeEntry.StopTime(), timeEntry.StopTime(), "Stop", source);
                })
                .SelectUnit();
        }

        private void logOutOfBoundsDate(DateTimeOffset? oldTime, DateTimeOffset? newTime, string startOrStopDate, string source)
        {
            var properties = new Dictionary<string, string>
            {
                { "Old Time", oldTime?.ToString() },
                { "New Time", newTime?.ToString() },
                { "Start or Stop", startOrStopDate },
                { "Source", source }
            };
            var exception = new OutOfBoundsDateTimeCreatedException("An out of bounds date was created", Environment.StackTrace);
            analyticsService.Track(exception, properties);
        }

        protected override IThreadSafeTimeEntry Convert(IDatabaseTimeEntry entity)
            => TimeEntry.From(entity);

        protected override ConflictResolutionMode ResolveConflicts(IDatabaseTimeEntry first, IDatabaseTimeEntry second)
            => Resolver.ForTimeEntries.Resolve(first, second);

        private IThreadSafeTimeEntry runningTimeEntry(IThreadSafeTimeEntry timeEntry)
        {
            timeEntry = timeEntry != null && timeEntry.IsRunning() ? timeEntry : null;
            currentlyRunningTimeEntryId = timeEntry?.Id;
            return timeEntry;
        }

        private IObservable<IThreadSafeTimeEntry> getCurrentlyRunningTimeEntry()
            => stopMultipleRunningTimeEntries()
                .SelectMany(_ => getAllRunning())
                .SelectMany(CommonFunctions.Identity)
                .SingleOrDefaultAsync();

        private IObservable<Unit> stopMultipleRunningTimeEntries()
            => getAllRunning()
                .Where(list => list.Count() > 1)
                .SelectMany(BatchUpdate)
                .Track(analyticsService.TwoRunningTimeEntriesInconsistencyFixed)
                .ToList()
                .SelectUnit()
                .DefaultIfEmpty(Unit.Default);

        private IObservable<IEnumerable<IThreadSafeTimeEntry>> getAllRunning()
            => GetAll(te => te.IsDeleted == false && te.Duration == null);
    }
}