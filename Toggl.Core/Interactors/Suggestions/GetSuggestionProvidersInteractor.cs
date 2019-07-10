using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Core.DataSources;
using Toggl.Core.Diagnostics;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Services;
using Toggl.Core.Suggestions;
using Toggl.Shared;
using Toggl.Storage.Settings;

namespace Toggl.Core.Interactors.Suggestions
{
    public class GetSuggestionProvidersInteractor : IInteractor<IObservable<IReadOnlyList<ISuggestionProvider>>>
    {
        private readonly int suggestionCount;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ITogglDataSource dataSource;
        private readonly ITimeService timeService;
        private readonly ICalendarService calendarService;
        private readonly IInteractorFactory interactorFactory;

        public GetSuggestionProvidersInteractor(
            int suggestionCount,
            IStopwatchProvider stopwatchProvider,
            ITogglDataSource dataSource,
            ITimeService timeService,
            ICalendarService calendarService,
            IInteractorFactory interactorFactory)
        {
            Ensure.Argument.IsInClosedRange(suggestionCount, 1, 9, nameof(suggestionCount));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.stopwatchProvider = stopwatchProvider;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.suggestionCount = suggestionCount;
            this.calendarService = calendarService;
            this.interactorFactory = interactorFactory;
        }

        public IObservable<IReadOnlyList<ISuggestionProvider>> Execute()
            => Observable.Return(
                new List<ISuggestionProvider>
                {
                    new RandomForestSuggestionProvider(stopwatchProvider, dataSource, timeService),
                    new CalendarSuggestionProvider(timeService, calendarService, interactorFactory),
                    new MostUsedTimeEntrySuggestionProvider(stopwatchProvider, timeService, dataSource, suggestionCount)
                }
            );
        }
}
