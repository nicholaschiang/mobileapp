using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Core.DataSources;
using Toggl.Core.Diagnostics;
using Toggl.Shared;
using Toggl.Storage;
using Toggl.Storage.Models;

namespace Toggl.Core.Suggestions
{
    public sealed class MostUsedTimeEntrySuggestionProvider : ISuggestionProvider
    {
        private const int daysToQuery = 42;
        private static readonly TimeSpan thresholdPeriod = TimeSpan.FromDays(daysToQuery);

        private readonly ITogglDataSource dataSource;
        private readonly ITimeService timeService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly int maxNumberOfSuggestions;

        public MostUsedTimeEntrySuggestionProvider(
            IStopwatchProvider stopwatchProvider,
            ITimeService timeService,
            ITogglDataSource dataSource,
            int maxNumberOfSuggestions)
        {
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.stopwatchProvider = stopwatchProvider;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.maxNumberOfSuggestions = maxNumberOfSuggestions;
        }

        public IObservable<Suggestion> GetSuggestions()
            => dataSource.TimeEntries
                .GetAll(isSuitableForSuggestion)
                .Do(_ => startStopwatch())
                .SelectMany(mostUsedTimeEntry)
                .Take(maxNumberOfSuggestions);

        private bool isSuitableForSuggestion(IDatabaseTimeEntry timeEntry)
        {
            var hasDescription = !string.IsNullOrWhiteSpace(timeEntry.Description);
            var hasProject = timeEntry.ProjectId.HasValue && !string.IsNullOrWhiteSpace(timeEntry.Project.Name);
            var isRecent = calculateDelta(timeEntry) <= thresholdPeriod;
            var isActive = isTimeEntryActive(timeEntry);
            var isSynced = timeEntry.SyncStatus == SyncStatus.InSync;

            return isRecent && isActive && isSynced && (hasDescription || hasProject);
        }

        private TimeSpan calculateDelta(IDatabaseTimeEntry timeEntry)
            => timeService.CurrentDateTime - timeEntry.Start;

        private bool isTimeEntryActive(IDatabaseTimeEntry timeEntry)
            => timeEntry.IsDeleted == false
               && !timeEntry.IsInaccessible
               && (timeEntry.Project?.Active ?? true);

        private IEnumerable<Suggestion> mostUsedTimeEntry(IEnumerable<IDatabaseTimeEntry> timeEntries)
        {
            var suggestions = timeEntries
                .GroupBy(te => new { te.Description, te.ProjectId, te.TaskId })
                .OrderByDescending(g => g.Count())
                .Select(grouping => grouping.First())
                .Select(timeEntry => new Suggestion(timeEntry, SuggestionProviderType.MostUsedTimeEntries));

            stopStopwatch();

            return suggestions;
        }

        private void startStopwatch()
        {
            stopwatchProvider.Remove(MeasuredOperation.MostUsedTimeEntriesPrediction);
            var mostUsedTimeEntriesPredictionStopWatch = stopwatchProvider.CreateAndStore(MeasuredOperation.MostUsedTimeEntriesPrediction);
            mostUsedTimeEntriesPredictionStopWatch.Start();
        }

        private void stopStopwatch()
        {
            var mostUsedTimeEntriesPredictionStopWatch = stopwatchProvider.Get(MeasuredOperation.MostUsedTimeEntriesPrediction);
            mostUsedTimeEntriesPredictionStopWatch?.Stop();
        }
    }
}
