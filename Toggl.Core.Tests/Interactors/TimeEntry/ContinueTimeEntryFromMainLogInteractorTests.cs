using NSubstitute;
using System;
using System.Reactive.Linq;
using Toggl.Core.Analytics;
using Toggl.Core.Extensions;
using Toggl.Core.Interactors;
using Toggl.Core.Models;
using Toggl.Core.Models.Interfaces;
using Toggl.Core.Tests.Mocks;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Core.Tests.Interactors.TimeEntry
{
    public sealed class ContinueTimeEntryFromMainLogInteractorTests
    {
        private IInteractorFactory interactorFactory = Substitute.For<IInteractorFactory>();

        private IAnalyticsService analyticsService = Substitute.For<IAnalyticsService>();

        private const long ProjectId = 10;

        private const long WorkspaceId = 11;

        private const long UserId = 12;

        private const long TaskId = 14;

        private long[] tagIds = { 15 };

        private const string validDescription = "Some random time entry";

        private DateTimeOffset startTime = new DateTimeOffset(2019, 6, 5, 14, 0, 0, TimeSpan.Zero);

        private IThreadSafeTimeEntry timeEntryToContinue => new MockTimeEntry
        {
            Id = 42,
            ProjectId = ProjectId,
            WorkspaceId = WorkspaceId,
            UserId = UserId,
            TaskId = TaskId,
            TagIds = tagIds,
            Description = validDescription,
            Start = startTime,
        };

        private IThreadSafeTimeEntry createdTimeEntry => new MockTimeEntry
        {
            Id = 43,
            ProjectId = ProjectId,
            WorkspaceId = WorkspaceId,
            UserId = UserId,
            TaskId = TaskId,
            TagIds = tagIds,
            Description = validDescription,
            Start = startTime,
        };

        private ITimeEntryPrototype timeEntryPrototype => timeEntryToContinue.AsTimeEntryPrototype();

        private const int IndexInLog = 5;

        private const int DayInLog = 1;

        private const int DaysInPast = 0;

        [Fact, LogIfTooSlow]
        public void CallsTheContinueTimeEntryInteractor()
        {
            var interactor = new ContinueTimeEntryFromMainLogInteractor(
                interactorFactory,
                analyticsService,
                timeEntryPrototype,
                ContinueTimeEntryMode.TimeEntriesGroupSwipe,
                IndexInLog,
                DayInLog,
                DaysInPast);

            interactor.Execute();

            interactorFactory
                .Received()
                .ContinueTimeEntry(Arg.Any<ITimeEntryPrototype>(), ContinueTimeEntryMode.TimeEntriesGroupSwipe)
                .Execute();
        }

        [Theory, LogIfTooSlow]
        [InlineData(ContinueTimeEntryMode.SingleTimeEntrySwipe, ContinueTimeEntryOrigin.Swipe)]
        [InlineData(ContinueTimeEntryMode.SingleTimeEntryContinueButton, ContinueTimeEntryOrigin.ContinueButton)]
        [InlineData(ContinueTimeEntryMode.TimeEntriesGroupSwipe, ContinueTimeEntryOrigin.GroupSwipe)]
        [InlineData(ContinueTimeEntryMode.TimeEntriesGroupContinueButton, ContinueTimeEntryOrigin.GroupContinueButton)]
        public async Task TracksTheTimeEntryContinuedEvent(ContinueTimeEntryMode continueMode, ContinueTimeEntryOrigin expectedOrigin)
        {
            interactorFactory
                .ContinueTimeEntry(Arg.Any<ITimeEntryPrototype>(), Arg.Any<ContinueTimeEntryMode>())
                .Execute()
                .Returns(Observable.Return(createdTimeEntry));

            var interactor = new ContinueTimeEntryFromMainLogInteractor(
                interactorFactory,
                analyticsService,
                timeEntryPrototype,
                continueMode,
                IndexInLog,
                DayInLog,
                DaysInPast);

            await interactor.Execute();

            analyticsService
                .Received()
                .TimeEntryContinued
                .Track(expectedOrigin, IndexInLog, DayInLog, DaysInPast);
        }
    }
}
